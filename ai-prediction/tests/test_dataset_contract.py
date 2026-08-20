"""Tests for the T-905 training dataset contract & preparation pipeline."""

from __future__ import annotations

import copy
import unittest
from pathlib import Path

from training import dataset_loader, prepare_dataset, target_mapping
from training.dataset_contract import (
    CANONICAL_TARGET_FIELD,
    FEATURE_SCHEMA_VERSION,
    RAW_TARGET_FIELD,
    SPLIT_RANDOM_STATE,
    SPLIT_TEST_SIZE,
    TRAINING_DATASET_VERSION,
)
from training.dataset_loader import DatasetValidationError
from training.prepare_dataset import LeakageGuardError
from training.target_mapping import TargetMappingSemanticError


def _valid_row(order_ref: str = "PO00001", sales_ref: str = "SO00001") -> dict:
    return {
        "productionOrderId": order_ref,
        "salesOrderId": sales_ref,
        "product": "Sandalye",
        "quantity": 16,
        "orderDate": "2026-06-08",
        "priority": "Acil",
        "stockLevelAtOrderTime": 352,
        "minimumStock": 80,
        "stockOrderRequired": False,
        "factoryWorkloadPercentAtOrderTime": 87,
        "factoryLoadAtOrderTime": "Yogun",
        "productionStartDate": "2026-06-08",
        "productionFinishDate": "2026-06-10",
        "packagingStartDate": "2026-06-10",
        "packagingFinishDate": "2026-06-11",
        "estimatedDeliveryDate": "2026-06-13",
        "orderProcessingDurationMinutes": 144,
        "procurementDurationMinutes": 0,
        "manufacturingDurationMinutes": 429,
        "packagingDurationMinutes": 122,
        "shippingDurationMinutes": 1236,
        "totalDeliveryDurationMinutes": 1931,
    }


def _valid_dataset(n: int = 4) -> list[dict]:
    rows = []
    for i in range(1, n + 1):
        rows.append(_valid_row(order_ref=f"PO{i:05d}", sales_ref=f"SO{i:05d}"))
    return rows


def _seed_orders(n: int = 4) -> dict:
    return {
        f"SO{i:05d}": {"id": f"SO{i:05d}", "productId": "P002", "quantity": 16}
        for i in range(1, n + 1)
    }


def _seed_boms() -> dict:
    return {"P002": 3}


class DatasetContractConstantsTests(unittest.TestCase):
    def test_versions_are_defined(self):
        self.assertEqual(FEATURE_SCHEMA_VERSION, 1)
        self.assertEqual(TRAINING_DATASET_VERSION, "synthetic-v1")

    def test_split_config(self):
        self.assertEqual(SPLIT_RANDOM_STATE, 42)
        self.assertEqual(SPLIT_TEST_SIZE, 0.20)

    def test_canonical_target(self):
        self.assertEqual(CANONICAL_TARGET_FIELD, "actual_total_working_lead_time_minutes")


class LoaderValidationTests(unittest.TestCase):
    def test_valid_dataset_passes(self):
        rows = _valid_dataset()
        dataset_loader.validate_row_count(rows)
        dataset_loader.validate_columns(rows)
        dataset_loader.analyze_nulls(rows)
        dataset_loader.analyze_duplicates(rows)
        dataset_loader.validate_numeric_types(rows)
        dataset_loader.validate_target(rows)  # no exception

    def test_empty_dataset_raises(self):
        with self.assertRaises(DatasetValidationError):
            dataset_loader.validate_row_count([])

    def test_missing_target_raises(self):
        rows = _valid_dataset(1)
        del rows[0][RAW_TARGET_FIELD]
        with self.assertRaises(DatasetValidationError):
            dataset_loader.validate_columns(rows)

    def test_invalid_target_type_raises(self):
        rows = _valid_dataset(1)
        rows[0][RAW_TARGET_FIELD] = "not-a-number"
        with self.assertRaises(DatasetValidationError):
            dataset_loader.validate_numeric_types(rows)

    def test_negative_target_raises(self):
        rows = _valid_dataset(1)
        rows[0][RAW_TARGET_FIELD] = -10
        with self.assertRaises(DatasetValidationError):
            dataset_loader.validate_target(rows)

    def test_zero_target_raises(self):
        rows = _valid_dataset(1)
        rows[0][RAW_TARGET_FIELD] = 0
        with self.assertRaises(DatasetValidationError):
            dataset_loader.validate_target(rows)

    def test_duplicate_order_reference_raises(self):
        rows = _valid_dataset(1) * 2
        with self.assertRaises(DatasetValidationError):
            dataset_loader.analyze_duplicates(rows)

    def test_null_required_column_raises(self):
        rows = _valid_dataset(1)
        rows[0]["quantity"] = None
        with self.assertRaises(DatasetValidationError):
            dataset_loader.analyze_nulls(rows)


def _blow_up_calendar_span(row: dict) -> dict:
    """Mutates a valid row so its target exceeds the orderDate->packagingFinishDate
    calendar span, while keeping the component-sum identity intact (adds the
    delta to shippingDurationMinutes so sum(stages) still equals the target)."""
    blown_up = 10 ** 7
    delta = blown_up - row[RAW_TARGET_FIELD]
    row["shippingDurationMinutes"] += delta
    row[RAW_TARGET_FIELD] = blown_up
    return row


class TargetMappingTests(unittest.TestCase):
    def test_target_mapping_success(self):
        rows = _valid_dataset()
        report = target_mapping.validate_target_mapping(rows)  # no exception
        self.assertEqual(report["violation_count"], 0)
        self.assertEqual(report["total_row_count"], len(rows))
        self.assertEqual(report["check"], target_mapping.CALENDAR_SPAN_CHECK_NAME)
        self.assertEqual(target_mapping.map_raw_target_to_canonical(rows[0]), 1931.0)

    def test_target_mapping_semantic_validation_failure_component_sum(self):
        rows = _valid_dataset(1)
        rows[0][RAW_TARGET_FIELD] = 9999  # no longer equals the sum of stage durations
        with self.assertRaises(TargetMappingSemanticError):
            target_mapping.validate_target_mapping(rows)

    def test_calendar_span_violation_does_not_raise(self):
        rows = _valid_dataset(1)
        _blow_up_calendar_span(rows[0])
        report = target_mapping.validate_target_mapping(rows)  # no exception
        self.assertEqual(report["violation_count"], 1)
        self.assertEqual(report["total_row_count"], 1)

    def test_calendar_span_violation_row_stays_in_dataset(self):
        rows = _valid_dataset(2)
        _blow_up_calendar_span(rows[0])
        report = target_mapping.validate_target_mapping(rows)
        self.assertEqual(report["violation_count"], 1)
        # The violating row is neither dropped nor mutated beyond the test's
        # own setup — it is still present and still maps to its raw target.
        self.assertEqual(len(rows), 2)
        self.assertEqual(
            target_mapping.map_raw_target_to_canonical(rows[0]), 10 ** 7
        )

    def test_calendar_span_warning_counter_matches_violations(self):
        rows = _valid_dataset(5)
        _blow_up_calendar_span(rows[1])
        _blow_up_calendar_span(rows[3])
        report = target_mapping.validate_target_mapping(rows)
        self.assertEqual(report["violation_count"], 2)
        self.assertEqual(report["total_row_count"], 5)

    def test_empty_dataset_raises(self):
        with self.assertRaises(TargetMappingSemanticError):
            target_mapping.validate_target_mapping([])


class LeakageGuardTests(unittest.TestCase):
    def test_leakage_column_present_raises(self):
        with self.assertRaises(LeakageGuardError):
            prepare_dataset.leakage_guard(["quantity", "productionStartDate"])

    def test_identifier_column_present_raises(self):
        with self.assertRaises(LeakageGuardError):
            prepare_dataset.leakage_guard(["quantity", "productionOrderId"])

    def test_target_field_present_raises(self):
        with self.assertRaises(LeakageGuardError):
            prepare_dataset.leakage_guard(["quantity", RAW_TARGET_FIELD])

    def test_resolved_features_pass(self):
        prepare_dataset.leakage_guard(prepare_dataset.resolve_feature_order())  # no exception


class PrepareDatasetPipelineTests(unittest.TestCase):
    def test_deterministic_feature_ordering(self):
        order_a = prepare_dataset.resolve_feature_order()
        order_b = prepare_dataset.resolve_feature_order()
        self.assertEqual(order_a, order_b)
        self.assertEqual(order_a, ["product_ref", "quantity", "bom_item_count"])

    def test_full_pipeline_on_valid_synthetic_dataset(self):
        ground_truth = _valid_dataset(6)
        seed_orders = _seed_orders(6)
        bom_item_counts = _seed_boms()

        target_mapping.validate_target_mapping(ground_truth)
        canonical_rows = prepare_dataset.build_canonical_rows(ground_truth, seed_orders, bom_item_counts)
        self.assertEqual(len(canonical_rows), 6)

        feature_order = prepare_dataset.resolve_feature_order()
        encodings = {
            "product_ref": prepare_dataset.build_categorical_encoding(
                [row["raw_features"]["product_ref"] for row in canonical_rows]
            )
        }
        processed = prepare_dataset.apply_preprocessing_contract(canonical_rows, encodings)
        for row in processed:
            self.assertEqual(len(row["x"]), len(feature_order))
            self.assertIsInstance(row["y"], float)

    def test_deterministic_split_reproducible(self):
        ground_truth = _valid_dataset(10)
        seed_orders = _seed_orders(10)
        bom_item_counts = _seed_boms()
        canonical_rows = prepare_dataset.build_canonical_rows(ground_truth, seed_orders, bom_item_counts)
        encodings = {
            "product_ref": prepare_dataset.build_categorical_encoding(
                [row["raw_features"]["product_ref"] for row in canonical_rows]
            )
        }
        processed = prepare_dataset.apply_preprocessing_contract(canonical_rows, encodings)

        train_a, test_a = prepare_dataset.deterministic_split(processed)
        train_b, test_b = prepare_dataset.deterministic_split(processed)

        self.assertEqual([r["order_ref"] for r in train_a], [r["order_ref"] for r in train_b])
        self.assertEqual([r["order_ref"] for r in test_a], [r["order_ref"] for r in test_b])
        self.assertEqual(len(train_a) + len(test_a), len(processed))

    def test_unknown_category_maps_to_reserved_code(self):
        encoding = prepare_dataset.build_categorical_encoding(["P001", "P002"])
        self.assertEqual(
            prepare_dataset.encode_categorical("P999", encoding),
            prepare_dataset.UNKNOWN_CATEGORY_CODE,
        )

    def test_build_canonical_rows_raises_on_missing_seed_order(self):
        ground_truth = _valid_dataset(1)
        with self.assertRaises(LeakageGuardError):
            prepare_dataset.build_canonical_rows(ground_truth, seed_orders={}, bom_item_counts={})

    def test_prepare_metadata_includes_target_mapping_warnings_and_keeps_violating_row(self):
        import json
        import tempfile

        ground_truth = _valid_dataset(4)
        _blow_up_calendar_span(ground_truth[2])  # one row violates calendar-span plausibility
        seed = {
            "orders": [
                {"id": o["id"], "productId": o["productId"], "quantity": o["quantity"]}
                for o in _seed_orders(4).values()
            ],
            "boms": [{"productId": "P002", "lines": [{}, {}, {}]}],
        }

        with tempfile.TemporaryDirectory() as tmp:
            gt_path = Path(tmp) / "prediction-ground-truth.json"
            seed_path = Path(tmp) / "mock-erp-seed.json"
            gt_path.write_text(json.dumps(ground_truth), encoding="utf-8")
            seed_path.write_text(json.dumps(seed), encoding="utf-8")

            result = prepare_dataset.prepare(gt_path, seed_path)

        warnings = result["metadata"]["target_mapping_warnings"]
        self.assertEqual(warnings["check"], target_mapping.CALENDAR_SPAN_CHECK_NAME)
        self.assertEqual(warnings["violation_count"], 1)
        self.assertEqual(warnings["total_row_count"], 4)
        # The violating row was not dropped: total prepared rows still equals input rows.
        self.assertEqual(len(result["train"]) + len(result["test"]), 4)

    def test_prepare_metadata_marks_default_fixture_as_preview(self):
        result = prepare_dataset.prepare(
            prepare_dataset.DEFAULT_GROUND_TRUTH, prepare_dataset.DEFAULT_SEED
        )
        self.assertTrue(result["metadata"]["source"]["is_preview_source"])
        self.assertEqual(
            result["metadata"]["source"]["ground_truth_path"],
            str(prepare_dataset.DEFAULT_GROUND_TRUTH),
        )

    def test_metadata_flags_preview_source(self):
        self.assertTrue(
            prepare_dataset._is_preview_source(prepare_dataset.DEFAULT_GROUND_TRUTH)
        )
        self.assertFalse(
            prepare_dataset._is_preview_source(Path("tests/Fixtures/ErpSeedConverterFull/prediction-ground-truth.json"))
        )


if __name__ == "__main__":
    unittest.main()
