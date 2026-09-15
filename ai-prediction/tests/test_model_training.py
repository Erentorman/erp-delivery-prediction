"""Tests for the T-907 XGBoost training pipeline (model_training.py).

Uses small synthetic datasets written to temp files and run through the real
prepare_dataset.prepare() + model_training.train() pipeline — not mocks —
so these tests exercise the actual T-905/T-907 contract end to end.
"""

from __future__ import annotations

import json
import math
import tempfile
import unittest
from datetime import date, timedelta
from pathlib import Path

from training import dataset_contract, model_training, prepare_dataset, promote_artifact
from training.dataset_loader import DatasetValidationError
from training.target_mapping import TargetMappingSemanticError

PRODUCTS = ["P001", "P002", "P003", "P004"]
BASE_ORDER_DATE = date(2026, 1, 5)


def _row(i: int, *, blow_up_calendar_span: bool = False, component_sum_mismatch: bool = False) -> dict:
    quantity = 5 + (i % 12)
    order_processing = 100 + i
    procurement = 0
    manufacturing = 200 + quantity * 10
    packaging = 50 + i
    shipping = 300 + (i % 7) * 20
    total = order_processing + procurement + manufacturing + packaging + shipping

    order_date = BASE_ORDER_DATE + timedelta(days=i % 30)
    # Generous 10-day span keeps the calendar-span check well satisfied by default.
    finish_date = order_date + timedelta(days=10)

    if blow_up_calendar_span:
        delta = 10 ** 7 - total
        shipping += delta
        total += delta

    if component_sum_mismatch:
        total += 9999  # breaks sum(stages) == total on purpose

    return {
        "productionOrderId": f"PO{i:05d}",
        "salesOrderId": f"SO{i:05d}",
        "product": "Sandalye",
        "quantity": quantity,
        "orderDate": order_date.isoformat(),
        "priority": "Acil",
        "stockLevelAtOrderTime": 100,
        "minimumStock": 40,
        "stockOrderRequired": False,
        "factoryWorkloadPercentAtOrderTime": 50,
        "factoryLoadAtOrderTime": "Normal",
        "productionStartDate": order_date.isoformat(),
        "productionFinishDate": finish_date.isoformat(),
        "packagingStartDate": finish_date.isoformat(),
        "packagingFinishDate": finish_date.isoformat(),
        "estimatedDeliveryDate": (finish_date + timedelta(days=2)).isoformat(),
        "orderProcessingDurationMinutes": order_processing,
        "procurementDurationMinutes": procurement,
        "manufacturingDurationMinutes": manufacturing,
        "packagingDurationMinutes": packaging,
        "shippingDurationMinutes": shipping,
        "totalDeliveryDurationMinutes": total,
    }


def _dataset(n: int, *, violate_calendar_span_index: int | None = None,
             violate_component_sum_index: int | None = None) -> list[dict]:
    return [
        _row(
            i,
            blow_up_calendar_span=(i == violate_calendar_span_index),
            component_sum_mismatch=(i == violate_component_sum_index),
        )
        for i in range(1, n + 1)
    ]


def _seed(n: int) -> dict:
    orders = [
        {"id": f"SO{i:05d}", "productId": PRODUCTS[i % 4], "quantity": 5 + (i % 12)}
        for i in range(1, n + 1)
    ]
    boms = [{"productId": p, "lines": [{}] * (2 + idx)} for idx, p in enumerate(PRODUCTS)]
    return {"orders": orders, "boms": boms}


def _write_dataset(tmp_dir: Path, ground_truth: list[dict], seed: dict) -> tuple[Path, Path]:
    gt_path = tmp_dir / "prediction-ground-truth.json"
    seed_path = tmp_dir / "mock-erp-seed.json"
    gt_path.write_text(json.dumps(ground_truth), encoding="utf-8")
    seed_path.write_text(json.dumps(seed), encoding="utf-8")
    return gt_path, seed_path


class TrainingPipelineTests(unittest.TestCase):
    def _train_on_synthetic(self, n: int = 30, **kwargs) -> dict:
        with tempfile.TemporaryDirectory() as tmp:
            gt_path, seed_path = _write_dataset(Path(tmp), _dataset(n, **kwargs), _seed(n))
            return model_training.train(gt_path, seed_path)

    def test_training_success(self):
        result = self._train_on_synthetic()
        self.assertIn("baseline", result["evaluation"])
        self.assertIn("xgboost", result["evaluation"])
        for section in ("baseline", "xgboost"):
            for metric in ("mae", "rmse", "r2"):
                self.assertIn(metric, result["evaluation"][section])

    def test_invalid_dataset_contract_raises(self):
        ground_truth = _dataset(5)
        del ground_truth[0]["orderDate"]  # violates REQUIRED_GROUND_TRUTH_COLUMNS
        with tempfile.TemporaryDirectory() as tmp:
            gt_path, seed_path = _write_dataset(Path(tmp), ground_truth, _seed(5))
            with self.assertRaises(DatasetValidationError):
                model_training.train(gt_path, seed_path)

    def test_missing_target_raises(self):
        ground_truth = _dataset(5)
        del ground_truth[0]["totalDeliveryDurationMinutes"]
        with tempfile.TemporaryDirectory() as tmp:
            gt_path, seed_path = _write_dataset(Path(tmp), ground_truth, _seed(5))
            with self.assertRaises(DatasetValidationError):
                model_training.train(gt_path, seed_path)

    def test_leakage_guard_regression(self):
        # Training must not have reintroduced leakage/identifier fields into
        # the resolved feature set used by the model.
        feature_order = prepare_dataset.resolve_feature_order()
        forbidden = (dataset_contract.LEAKAGE_FIELDS | dataset_contract.IDENTIFIER_FIELDS)
        self.assertFalse(set(feature_order) & forbidden)
        result = self._train_on_synthetic()
        self.assertEqual(len(result["model_metadata"]["feature_order"]), 3)

    def test_approved_feature_set(self):
        self.assertEqual(
            prepare_dataset.resolve_feature_order(),
            ["product_ref", "quantity", "bom_item_count"],
        )

    def test_deterministic_feature_order(self):
        self.assertEqual(prepare_dataset.resolve_feature_order(), prepare_dataset.resolve_feature_order())

    def test_reproducible_split_and_metrics(self):
        with tempfile.TemporaryDirectory() as tmp:
            gt_path, seed_path = _write_dataset(Path(tmp), _dataset(30), _seed(30))
            result_a = model_training.train(gt_path, seed_path)
            result_b = model_training.train(gt_path, seed_path)

        self.assertEqual(result_a["evaluation"], result_b["evaluation"])

        meta_a = dict(result_a["model_metadata"])
        meta_b = dict(result_b["model_metadata"])
        meta_a.pop("training_timestamp_utc")
        meta_b.pop("training_timestamp_utc")
        self.assertEqual(meta_a, meta_b)

    def test_baseline_fit(self):
        baseline = model_training.MeanBaselineRegressor().fit([10.0, 20.0, 30.0])
        self.assertEqual(baseline.predict(3), [20.0, 20.0, 20.0])

    def test_xgboost_model_fit(self):
        result = self._train_on_synthetic()
        self.assertEqual(type(result["model"]).__name__, "XGBRegressor")

    def test_prediction_numeric(self):
        result = self._train_on_synthetic()
        with tempfile.TemporaryDirectory() as tmp:
            gt_path, seed_path = _write_dataset(Path(tmp), _dataset(30), _seed(30))
            prepared = prepare_dataset.prepare(gt_path, seed_path)
        X_test = [row["x"] for row in prepared["test"]]
        preds = result["model"].predict(X_test)
        for p in preds:
            self.assertIsInstance(float(p), float)

    def test_prediction_finite(self):
        result = self._train_on_synthetic()
        with tempfile.TemporaryDirectory() as tmp:
            gt_path, seed_path = _write_dataset(Path(tmp), _dataset(30), _seed(30))
            prepared = prepare_dataset.prepare(gt_path, seed_path)
        X_test = [row["x"] for row in prepared["test"]]
        preds = result["model"].predict(X_test)
        for p in preds:
            self.assertTrue(math.isfinite(float(p)))

    def test_artifact_save(self):
        result = self._train_on_synthetic()
        with tempfile.TemporaryDirectory() as tmp:
            out_dir = Path(tmp) / "artifact"
            model_training.save_artifacts(result, out_dir)
            self.assertTrue((out_dir / "model.json").exists())
            self.assertTrue((out_dir / "model-metadata.json").exists())
            self.assertTrue((out_dir / "evaluation.json").exists())

    def test_artifact_reload(self):
        result = self._train_on_synthetic()
        with tempfile.TemporaryDirectory() as tmp:
            out_dir = Path(tmp) / "artifact"
            model_training.save_artifacts(result, out_dir)
            reloaded = model_training.load_model(out_dir / "model.json")
            self.assertEqual(type(reloaded).__name__, "XGBRegressor")

    def test_reload_prediction_matches_original(self):
        result = self._train_on_synthetic()
        sample = result["X_test_sample"]
        with tempfile.TemporaryDirectory() as tmp:
            out_dir = Path(tmp) / "artifact"
            model_training.save_artifacts(result, out_dir)
            reloaded = model_training.load_model(out_dir / "model.json")

        original_pred = float(result["model"].predict([sample])[0])
        reloaded_pred = float(reloaded.predict([sample])[0])
        self.assertAlmostEqual(original_pred, reloaded_pred, places=4)
        self.assertTrue(math.isfinite(reloaded_pred))

    def test_metadata_validation(self):
        result = self._train_on_synthetic()
        required_keys = {
            "model_version", "model_type", "feature_schema_version",
            "training_dataset_version", "target_name", "feature_order",
            "categorical_encodings", "unknown_category_code",
            "training_timestamp_utc", "split", "train_row_count",
            "test_row_count", "dataset_source", "target_mapping_warnings",
            "metrics",
        }
        self.assertTrue(required_keys.issubset(result["model_metadata"].keys()))
        self.assertIn("baseline", result["model_metadata"]["metrics"])
        self.assertIn("xgboost", result["model_metadata"]["metrics"])

    def test_feature_schema_version_validation(self):
        result = self._train_on_synthetic()
        self.assertEqual(
            result["model_metadata"]["feature_schema_version"],
            dataset_contract.FEATURE_SCHEMA_VERSION,
        )

    def test_training_dataset_version_validation(self):
        result = self._train_on_synthetic()
        self.assertEqual(
            result["model_metadata"]["training_dataset_version"],
            dataset_contract.TRAINING_DATASET_VERSION,
        )

    def test_calendar_span_warning_rows_remain_usable(self):
        result = self._train_on_synthetic(n=30, violate_calendar_span_index=5)
        self.assertEqual(result["model_metadata"]["target_mapping_warnings"]["violation_count"], 1)
        self.assertEqual(
            result["model_metadata"]["train_row_count"] + result["model_metadata"]["test_row_count"],
            30,
        )

    def test_component_sum_violation_remains_fatal(self):
        with tempfile.TemporaryDirectory() as tmp:
            gt_path, seed_path = _write_dataset(
                Path(tmp), _dataset(10, violate_component_sum_index=3), _seed(10)
            )
            with self.assertRaises(TargetMappingSemanticError):
                model_training.train(gt_path, seed_path)


class PromoteArtifactTests(unittest.TestCase):
    """promote_artifact.py is the only writer of the canonical
    ai-prediction/model/artifacts/<model_version>/ location — these tests
    monkeypatch CANONICAL_ARTIFACT_ROOT so they never touch the real,
    committed artifact directory."""

    def _fake_staged_artifact(self, tmp: Path, model_version: str = "xgb-test") -> Path:
        source_dir = tmp / "staged"
        source_dir.mkdir()
        (source_dir / "model.json").write_text('{"fake": "model"}', encoding="utf-8")
        (source_dir / "model-metadata.json").write_text(
            json.dumps({"model_version": model_version, "feature_order": ["product_ref"]}),
            encoding="utf-8",
        )
        (source_dir / "evaluation.json").write_text('{"xgboost": {"mae": 1.0}}', encoding="utf-8")
        return source_dir

    def test_promote_copies_three_files_to_canonical_location(self):
        with tempfile.TemporaryDirectory() as tmp:
            tmp_path = Path(tmp)
            source_dir = self._fake_staged_artifact(tmp_path)
            fake_canonical_root = tmp_path / "model" / "artifacts"

            original_root = promote_artifact.CANONICAL_ARTIFACT_ROOT
            promote_artifact.CANONICAL_ARTIFACT_ROOT = fake_canonical_root
            try:
                dest_dir = promote_artifact.promote(source_dir, model_version="xgb-test")
            finally:
                promote_artifact.CANONICAL_ARTIFACT_ROOT = original_root

            self.assertEqual(dest_dir, fake_canonical_root / "xgb-test")
            for name in promote_artifact.ARTIFACT_FILES:
                self.assertTrue((dest_dir / name).exists())
            self.assertEqual((dest_dir / "model.json").read_text(), '{"fake": "model"}')

    def test_promote_infers_model_version_from_metadata(self):
        with tempfile.TemporaryDirectory() as tmp:
            tmp_path = Path(tmp)
            source_dir = self._fake_staged_artifact(tmp_path, model_version="xgb-v9.9")
            fake_canonical_root = tmp_path / "model" / "artifacts"

            original_root = promote_artifact.CANONICAL_ARTIFACT_ROOT
            promote_artifact.CANONICAL_ARTIFACT_ROOT = fake_canonical_root
            try:
                dest_dir = promote_artifact.promote(source_dir)  # no explicit model_version
            finally:
                promote_artifact.CANONICAL_ARTIFACT_ROOT = original_root

            self.assertEqual(dest_dir.name, "xgb-v9.9")

    def test_promote_raises_on_missing_file(self):
        with tempfile.TemporaryDirectory() as tmp:
            tmp_path = Path(tmp)
            source_dir = self._fake_staged_artifact(tmp_path)
            (source_dir / "evaluation.json").unlink()

            with self.assertRaises(FileNotFoundError):
                promote_artifact.promote(source_dir, model_version="xgb-test")


if __name__ == "__main__":
    unittest.main()
