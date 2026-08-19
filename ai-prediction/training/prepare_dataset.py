"""Dataset preparation pipeline (T-905).

Raw ERP Dataset -> Dataset Loader -> Schema Validation -> Ground-Truth
Validation -> Leakage Guard -> Feature Availability Resolution ->
Canonical Dataset -> Deterministic Split -> (T-907 Model Training)

This module does NOT train a model. Running it produces a canonical,
leakage-free, reproducible dataset + metadata for T-907 to consume.

Usage:
    python -m training.prepare_dataset \
        --ground-truth <path to prediction-ground-truth.json> \
        --seed <path to mock-erp-seed.json> \
        --out <output directory>

Defaults point at the repo's committed preview fixture, since no "full"
dataset is checked into the repo (the source Excel is intentionally not
committed; see tools/erp-seed-converter/README.md).
"""

from __future__ import annotations

import argparse
import json
import random
from pathlib import Path

from training.dataset_contract import (
    FEATURE_CONTRACT,
    FEATURE_CONTRACT_BY_NAME,
    FEATURE_SCHEMA_VERSION,
    FeatureAvailability,
    FeatureType,
    IDENTIFIER_FIELDS,
    LEAKAGE_FIELDS,
    RAW_TARGET_FIELD,
    SPLIT_RANDOM_STATE,
    SPLIT_TEST_SIZE,
    TRAINING_DATASET_VERSION,
    CANONICAL_TARGET_FIELD,
)
from training.dataset_loader import load_ground_truth, load_seed_boms, load_seed_orders
from training.target_mapping import map_raw_target_to_canonical, validate_target_mapping

REPO_ROOT = Path(__file__).resolve().parents[2]
DEFAULT_GROUND_TRUTH = (
    REPO_ROOT
    / "tests/App.Integration.Tests/Fixtures/ErpSeedConverterPreview/prediction-ground-truth.json"
)
DEFAULT_SEED = (
    REPO_ROOT / "tests/App.Integration.Tests/Fixtures/ErpSeedConverterPreview/mock-erp-seed.json"
)


class LeakageGuardError(Exception):
    """Raised if a leakage or identifier field would end up in the feature set."""


def resolve_feature_order() -> list[str]:
    """Deterministic, contract-declared feature order for resolved (non-UNAVAILABLE) features.

    OPTIONAL features (currently only product_category, always null in the
    MVP source) are excluded here since they carry no information yet; they
    remain declared in FEATURE_CONTRACT for Phase-2.
    """
    return [
        spec.name
        for spec in FEATURE_CONTRACT
        if spec.availability is FeatureAvailability.AVAILABLE
    ]


def leakage_guard(feature_names: list[str]) -> None:
    forbidden = (LEAKAGE_FIELDS | IDENTIFIER_FIELDS) & set(feature_names)
    if forbidden:
        raise LeakageGuardError(f"Leakage/identifier fields present in feature set: {sorted(forbidden)}")
    if CANONICAL_TARGET_FIELD in feature_names or RAW_TARGET_FIELD in feature_names:
        raise LeakageGuardError("Target field present in feature set.")


def build_categorical_encoding(values: list[str]) -> dict[str, int]:
    """Deterministic encoding: sorted unique values -> 0..N-1. 'unknown' unseen category reserved as -1."""
    unique_sorted = sorted(set(values))
    return {value: index for index, value in enumerate(unique_sorted)}


UNKNOWN_CATEGORY_CODE = -1


def encode_categorical(value: str, encoding: dict[str, int]) -> int:
    return encoding.get(value, UNKNOWN_CATEGORY_CODE)


def build_canonical_rows(
    ground_truth: list[dict],
    seed_orders: dict[str, dict],
    bom_item_counts: dict[str, int],
) -> list[dict]:
    """Joins ground-truth with the seed to produce raw feature values per resolved feature.

    Only fields declared AVAILABLE in FEATURE_CONTRACT are populated here.
    """
    feature_order = resolve_feature_order()
    leakage_guard(feature_order)

    rows = []
    for gt_row in ground_truth:
        order_id = gt_row["salesOrderId"]
        seed_order = seed_orders.get(order_id)
        if seed_order is None:
            raise LeakageGuardError(
                f"{gt_row['productionOrderId']}: no matching order '{order_id}' in mock-erp-seed.json."
            )

        product_id = seed_order["productId"]
        raw_features = {
            "product_ref": product_id,
            "quantity": seed_order["quantity"],
            "bom_item_count": bom_item_counts.get(product_id, 0),
        }
        missing = set(feature_order) - raw_features.keys()
        if missing:
            raise LeakageGuardError(f"Resolved feature(s) not populated by build_canonical_rows: {missing}")

        rows.append(
            {
                "order_ref": gt_row["productionOrderId"],
                "raw_features": raw_features,
                "target": map_raw_target_to_canonical(gt_row),
            }
        )
    return rows


def apply_preprocessing_contract(
    rows: list[dict], categorical_encodings: dict[str, dict[str, int]]
) -> list[dict]:
    """Applies the numeric/categorical preprocessing contract, in deterministic feature order.

    Numeric: pass through as float, already validated finite/non-negative by dataset_loader.
    Categorical: deterministic sorted-unique encoding; unseen categories map to UNKNOWN_CATEGORY_CODE.
    """
    feature_order = resolve_feature_order()

    processed = []
    for row in rows:
        x = []
        for name in feature_order:
            spec = FEATURE_CONTRACT_BY_NAME[name]
            raw_value = row["raw_features"][name]
            if spec.feature_type is FeatureType.CATEGORICAL:
                x.append(encode_categorical(raw_value, categorical_encodings[name]))
            else:
                x.append(float(raw_value))
        processed.append({"order_ref": row["order_ref"], "x": x, "y": row["target"]})
    return processed


def deterministic_split(
    processed_rows: list[dict], test_size: float = SPLIT_TEST_SIZE, random_state: int = SPLIT_RANDOM_STATE
) -> tuple[list[dict], list[dict]]:
    """Deterministic train/test split: same dataset_version + split config -> same rows every run."""
    ordered = sorted(processed_rows, key=lambda r: r["order_ref"])
    indices = list(range(len(ordered)))
    random.Random(random_state).shuffle(indices)

    test_count = round(len(ordered) * test_size)
    test_indices = set(indices[:test_count])

    train = [ordered[i] for i in range(len(ordered)) if i not in test_indices]
    test = [ordered[i] for i in range(len(ordered)) if i in test_indices]
    return train, test


def _is_preview_source(ground_truth_path: Path) -> bool:
    return any("preview" in part.lower() for part in ground_truth_path.parts)


def prepare(ground_truth_path: Path, seed_path: Path) -> dict:
    ground_truth = load_ground_truth(ground_truth_path)
    validate_target_mapping(ground_truth)

    seed_orders = load_seed_orders(seed_path)
    bom_item_counts = load_seed_boms(seed_path)

    canonical_rows = build_canonical_rows(ground_truth, seed_orders, bom_item_counts)

    feature_order = resolve_feature_order()
    categorical_encodings = {
        name: build_categorical_encoding([row["raw_features"][name] for row in canonical_rows])
        for name in feature_order
        if FEATURE_CONTRACT_BY_NAME[name].feature_type is FeatureType.CATEGORICAL
    }

    processed_rows = apply_preprocessing_contract(canonical_rows, categorical_encodings)
    train_rows, test_rows = deterministic_split(processed_rows)

    metadata = {
        "feature_schema_version": FEATURE_SCHEMA_VERSION,
        "training_dataset_version": TRAINING_DATASET_VERSION,
        "canonical_target_field": CANONICAL_TARGET_FIELD,
        "raw_target_field": RAW_TARGET_FIELD,
        "feature_order": feature_order,
        "categorical_encodings": categorical_encodings,
        "unknown_category_code": UNKNOWN_CATEGORY_CODE,
        "split": {
            "random_state": SPLIT_RANDOM_STATE,
            "test_size": SPLIT_TEST_SIZE,
            "train_count": len(train_rows),
            "test_count": len(test_rows),
        },
        "row_count": len(processed_rows),
        "source": {
            "ground_truth_path": str(ground_truth_path),
            "seed_path": str(seed_path),
            "is_preview_source": _is_preview_source(ground_truth_path),
        },
    }

    return {
        "metadata": metadata,
        "train": train_rows,
        "test": test_rows,
    }


def main() -> None:
    parser = argparse.ArgumentParser(description="Prepare the AI training dataset (T-905).")
    parser.add_argument("--ground-truth", type=Path, default=DEFAULT_GROUND_TRUTH)
    parser.add_argument("--seed", type=Path, default=DEFAULT_SEED)
    parser.add_argument("--out", type=Path, default=Path(__file__).resolve().parent / "output")
    args = parser.parse_args()

    result = prepare(args.ground_truth, args.seed)

    args.out.mkdir(parents=True, exist_ok=True)
    (args.out / "metadata.json").write_text(
        json.dumps(result["metadata"], indent=2, ensure_ascii=False), encoding="utf-8"
    )
    (args.out / "train.json").write_text(
        json.dumps(result["train"], indent=2, ensure_ascii=False), encoding="utf-8"
    )
    (args.out / "test.json").write_text(
        json.dumps(result["test"], indent=2, ensure_ascii=False), encoding="utf-8"
    )

    print(f"Wrote {result['metadata']['row_count']} rows "
          f"({result['metadata']['split']['train_count']} train / "
          f"{result['metadata']['split']['test_count']} test) to {args.out}")

    if result["metadata"]["source"]["is_preview_source"]:
        print(
            "WARNING: source dataset is the ErpSeedConverterPreview fixture "
            f"({result['metadata']['row_count']} rows), not a full training dataset. "
            f"training_dataset_version='{TRAINING_DATASET_VERSION}' describes the schema/contract, "
            "not dataset completeness — do not train T-907 on this output as if it were final."
        )


if __name__ == "__main__":
    main()
