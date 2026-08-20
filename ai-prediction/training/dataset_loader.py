"""Deterministic loading and structural validation of the raw training source.

Raw source = tools/erp-seed-converter output:
  - prediction-ground-truth.json (target + order-time snapshot + leakage fields)
  - mock-erp-seed.json           (orders[].productId, boms[] for bom_item_count)

This module only validates structure (row count, required columns, null
values, duplicates, numeric types, target sanity). Target *semantic*
validation lives in target_mapping.py; leakage/feature resolution lives in
prepare_dataset.py.
"""

from __future__ import annotations

import json
import math
from pathlib import Path

from training.dataset_contract import (
    IDENTIFIER_FIELDS,
    LEAKAGE_FIELDS,
    RAW_STAGE_DURATION_FIELDS,
    RAW_TARGET_FIELD,
)

# Columns every ground-truth row must have. Superset of leakage/identifier
# fields plus the raw feature-relevant fields (product, quantity, orderDate).
REQUIRED_GROUND_TRUTH_COLUMNS = frozenset(
    {
        "productionOrderId",
        "salesOrderId",
        "product",
        "quantity",
        "orderDate",
        RAW_TARGET_FIELD,
    }
    | LEAKAGE_FIELDS
    | IDENTIFIER_FIELDS
)

NUMERIC_COLUMNS = frozenset({"quantity", RAW_TARGET_FIELD} | set(RAW_STAGE_DURATION_FIELDS))


class DatasetValidationError(Exception):
    """Raised for any structural defect in the raw dataset."""


def load_ground_truth(path: Path) -> list[dict]:
    if not path.exists():
        raise DatasetValidationError(f"Ground-truth file not found: {path}")

    with path.open(encoding="utf-8") as f:
        data = json.load(f)

    if not isinstance(data, list):
        raise DatasetValidationError(
            f"Ground-truth file must contain a JSON array of records, got {type(data).__name__}."
        )

    validate_row_count(data)
    validate_columns(data)
    analyze_nulls(data)
    analyze_duplicates(data)
    validate_numeric_types(data)
    validate_target(data)

    return data


def load_seed_orders(path: Path) -> dict[str, dict]:
    """Returns orders[] from mock-erp-seed.json keyed by order id."""
    if not path.exists():
        raise DatasetValidationError(f"Seed file not found: {path}")

    with path.open(encoding="utf-8") as f:
        seed = json.load(f)

    orders = seed.get("orders")
    if not isinstance(orders, list):
        raise DatasetValidationError("mock-erp-seed.json is missing an orders[] array.")

    return {order["id"]: order for order in orders}


def load_seed_boms(path: Path) -> dict[str, int]:
    """Returns bom_item_count per productId from mock-erp-seed.json boms[]."""
    with path.open(encoding="utf-8") as f:
        seed = json.load(f)

    boms = seed.get("boms")
    if not isinstance(boms, list):
        raise DatasetValidationError("mock-erp-seed.json is missing a boms[] array.")

    return {bom["productId"]: len(bom.get("lines", [])) for bom in boms}


def validate_row_count(rows: list[dict]) -> None:
    if len(rows) == 0:
        raise DatasetValidationError("Ground-truth dataset is empty (0 rows).")


def validate_columns(rows: list[dict]) -> None:
    for i, row in enumerate(rows):
        missing = REQUIRED_GROUND_TRUTH_COLUMNS - row.keys()
        if missing:
            raise DatasetValidationError(
                f"Row {i} ({row.get('productionOrderId', '?')}) missing required columns: "
                f"{sorted(missing)}"
            )


def analyze_nulls(rows: list[dict]) -> None:
    for i, row in enumerate(rows):
        null_columns = [
            col
            for col in REQUIRED_GROUND_TRUTH_COLUMNS
            if row.get(col) is None
        ]
        if null_columns:
            raise DatasetValidationError(
                f"Row {i} ({row.get('productionOrderId', '?')}) has null required columns: "
                f"{sorted(null_columns)}"
            )


def analyze_duplicates(rows: list[dict]) -> None:
    seen: set[str] = set()
    for row in rows:
        ref = row.get("productionOrderId")
        if ref in seen:
            raise DatasetValidationError(f"Duplicate productionOrderId: {ref}")
        seen.add(ref)


def validate_numeric_types(rows: list[dict]) -> None:
    for row in rows:
        ref = row.get("productionOrderId", "?")
        for col in NUMERIC_COLUMNS:
            value = row.get(col)
            if isinstance(value, bool) or not isinstance(value, (int, float)):
                raise DatasetValidationError(
                    f"{ref}: column '{col}' must be numeric, got {value!r}."
                )
            if not math.isfinite(value):
                raise DatasetValidationError(f"{ref}: column '{col}' is not finite: {value!r}.")


def validate_target(rows: list[dict]) -> None:
    for row in rows:
        ref = row.get("productionOrderId", "?")
        target = row[RAW_TARGET_FIELD]
        if target < 0:
            raise DatasetValidationError(
                f"{ref}: {RAW_TARGET_FIELD} is negative ({target}); a delivery duration cannot be negative."
            )
        if target == 0:
            raise DatasetValidationError(
                f"{ref}: {RAW_TARGET_FIELD} is zero; treated as invalid for training purposes."
            )
