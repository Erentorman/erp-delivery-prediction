"""Raw target -> canonical target mapping and its semantic validation.

Per T-905 spec: the presence of `totalDeliveryDurationMinutes` in the raw
ground-truth data does NOT by itself mean it is the canonical target
(`actual_total_working_lead_time_minutes`). The semantic equivalence is
checked automatically before any mapping is applied. If validation fails,
this module raises — the pipeline must not silently proceed to training.
"""

from __future__ import annotations

from datetime import date

from training.dataset_contract import (
    CANONICAL_TARGET_FIELD,
    RAW_STAGE_DURATION_FIELDS,
    RAW_TARGET_FIELD,
)


class TargetMappingSemanticError(Exception):
    """Raised when the raw target cannot be trusted as the canonical target."""


def _parse_date(value: str) -> date:
    return date.fromisoformat(value)


def validate_target_mapping(rows: list[dict]) -> None:
    """Validates that RAW_TARGET_FIELD ⇔ CANONICAL_TARGET_FIELD semantically.

    Two independent checks, both must hold for every row:

    1. Component-sum check: RAW_TARGET_FIELD must equal the sum of
       RAW_STAGE_DURATION_FIELDS. This confirms the raw target represents a
       total of realized stage durations, not an unrelated number.
    2. Working-minutes plausibility check: RAW_TARGET_FIELD must not exceed
       the calendar-minute span between the order date and the last known
       realized date on the row. Working minutes are a subset of calendar
       minutes; if the raw target exceeds the calendar span it cannot be a
       working-minutes duration and the "actual_total_working_lead_time"
       semantic claim is false.

    Raises TargetMappingSemanticError on the first row that fails either
    check. Does not mutate rows and performs no I/O.
    """
    if not rows:
        raise TargetMappingSemanticError("Cannot validate target mapping: dataset is empty.")

    for row in rows:
        order_ref = row.get("productionOrderId", "<unknown>")

        raw_target = row.get(RAW_TARGET_FIELD)
        if not isinstance(raw_target, (int, float)) or isinstance(raw_target, bool):
            raise TargetMappingSemanticError(
                f"{order_ref}: {RAW_TARGET_FIELD} is missing or non-numeric."
            )

        stage_values = []
        for field in RAW_STAGE_DURATION_FIELDS:
            value = row.get(field)
            if not isinstance(value, (int, float)) or isinstance(value, bool):
                raise TargetMappingSemanticError(
                    f"{order_ref}: stage duration field '{field}' is missing or non-numeric."
                )
            stage_values.append(value)

        stage_sum = sum(stage_values)
        if stage_sum != raw_target:
            raise TargetMappingSemanticError(
                f"{order_ref}: component-sum check failed — sum({list(RAW_STAGE_DURATION_FIELDS)}) "
                f"= {stage_sum} != {RAW_TARGET_FIELD} = {raw_target}. "
                f"Cannot map {RAW_TARGET_FIELD} -> {CANONICAL_TARGET_FIELD}."
            )

        try:
            order_date = _parse_date(row["orderDate"])
            finish_date = _parse_date(row["packagingFinishDate"])
        except (KeyError, TypeError, ValueError) as exc:
            raise TargetMappingSemanticError(
                f"{order_ref}: cannot evaluate calendar-span plausibility check "
                f"(missing/invalid orderDate or packagingFinishDate): {exc}"
            ) from exc

        calendar_span_minutes = (finish_date - order_date).days * 24 * 60
        if raw_target > calendar_span_minutes:
            raise TargetMappingSemanticError(
                f"{order_ref}: working-minutes plausibility check failed — "
                f"{RAW_TARGET_FIELD} = {raw_target} exceeds calendar span "
                f"{calendar_span_minutes} minutes between orderDate and "
                f"packagingFinishDate. A working-minutes value cannot exceed "
                f"the calendar span it was measured within."
            )


def map_raw_target_to_canonical(row: dict) -> float:
    """Returns the canonical target value for one raw ground-truth row.

    Callers MUST call validate_target_mapping() over the full dataset first;
    this function does not re-validate per-row.
    """
    return float(row[RAW_TARGET_FIELD])
