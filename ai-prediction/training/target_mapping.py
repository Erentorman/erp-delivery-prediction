"""Raw target -> canonical target mapping and its semantic validation.

Per T-905 spec: the presence of `totalDeliveryDurationMinutes` in the raw
ground-truth data does NOT by itself mean it is the canonical target
(`actual_total_working_lead_time_minutes`). The semantic equivalence is
checked automatically before any mapping is applied. The check that proves
the mapping mathematically (component-sum) raises on failure — the pipeline
must not silently proceed to training on a dataset where it doesn't hold.
The calendar-span check is a plausibility signal, not proof; its violations
are reported, not raised — see validate_target_mapping() docstring.
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


# Name recorded in the report returned by validate_target_mapping() and echoed
# into prepare_dataset metadata, so the check is identifiable in output/logs.
CALENDAR_SPAN_CHECK_NAME = "target_calendar_span_plausibility"


def _parse_date(value: str) -> date:
    return date.fromisoformat(value)


def validate_target_mapping(rows: list[dict]) -> dict:
    """Validates that RAW_TARGET_FIELD ⇔ CANONICAL_TARGET_FIELD semantically.

    Two checks run per row, with different severity:

    1. Component-sum check (FATAL): RAW_TARGET_FIELD must equal the sum of
       RAW_STAGE_DURATION_FIELDS. This confirms the raw target represents a
       total of realized stage durations, not an unrelated number. Raises
       TargetMappingSemanticError immediately on the first row that fails —
       this is the check that proves the raw->canonical target mapping is
       mathematically sound, and the pipeline must not proceed on a dataset
       where it doesn't hold.
    2. Calendar-span plausibility check (NON-FATAL / data-quality warning):
       RAW_TARGET_FIELD compared against the calendar-minute span between
       orderDate and packagingFinishDate. This is a plausibility signal, not
       proof the mapping is wrong — the calendar span does not account for
       time elapsed after packagingFinishDate (e.g. shipping), so a
       violation does not by itself invalidate the target. Violations are
       counted, not raised: the row stays in the dataset unchanged.

    Returns a report dict for the calendar-span check:
        {"check": CALENDAR_SPAN_CHECK_NAME, "violation_count": int, "total_row_count": int}

    Does not mutate rows and performs no I/O.
    """
    if not rows:
        raise TargetMappingSemanticError("Cannot validate target mapping: dataset is empty.")

    calendar_span_violation_count = 0

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
            # Non-fatal: recorded as a data-quality warning, not raised. The
            # row is kept as-is — see CALENDAR_SPAN_CHECK_NAME docstring above.
            calendar_span_violation_count += 1

    return {
        "check": CALENDAR_SPAN_CHECK_NAME,
        "violation_count": calendar_span_violation_count,
        "total_row_count": len(rows),
    }


def map_raw_target_to_canonical(row: dict) -> float:
    """Returns the canonical target value for one raw ground-truth row.

    Callers MUST call validate_target_mapping() over the full dataset first;
    this function does not re-validate per-row.
    """
    return float(row[RAW_TARGET_FIELD])
