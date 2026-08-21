"""Tests for the T-909 FastAPI inference service (main.py, model/loader.py,
preprocessing/features.py).

Exercises the real canonical xgb-v0.1 artifact end to end via FastAPI's
TestClient (no HTTP mocks) plus focused unit tests for metadata-driven
feature preparation and artifact loading failure modes.
"""

from __future__ import annotations

import inspect
import json
import math
import tempfile
import unittest
from pathlib import Path

from fastapi.testclient import TestClient

import main
from model import loader
from preprocessing.features import FeatureValidationError, prepare_feature_vector

CANONICAL_ARTIFACT_DIR = loader.DEFAULT_MODEL_ARTIFACT_DIR
CANONICAL_METADATA = json.loads((CANONICAL_ARTIFACT_DIR / "model-metadata.json").read_text(encoding="utf-8"))


def _valid_features() -> dict:
    return {"product_ref": "P001", "quantity": 5, "bom_item_count": 3}


class HealthAndPredictTests(unittest.TestCase):
    def setUp(self):
        # Fresh ServiceState + TestClient per test so lifespan reloads the
        # artifact exactly once for this test's client.
        main.state = main.ServiceState()
        self.client = TestClient(main.app)
        self.client.__enter__()

    def tearDown(self):
        self.client.__exit__(None, None, None)

    def test_health_model_loaded(self):
        response = self.client.get("/health")
        self.assertEqual(response.status_code, 200)
        self.assertEqual(response.json(), {"status": "healthy"})

    def test_predict_success(self):
        response = self.client.post(
            "/predict",
            json={"featureSchemaVersion": "1", "features": _valid_features()},
        )
        self.assertEqual(response.status_code, 200)
        body = response.json()
        self.assertIn("workingLeadTimeMinutes", body)
        self.assertEqual(body["modelVersion"], CANONICAL_METADATA["model_version"])
        self.assertEqual(body["featureSchemaVersion"], str(CANONICAL_METADATA["feature_schema_version"]))
        self.assertEqual(body["trainingDatasetVersion"], CANONICAL_METADATA["training_dataset_version"])

    def test_prediction_finite(self):
        response = self.client.post(
            "/predict",
            json={"featureSchemaVersion": "1", "features": _valid_features()},
        )
        self.assertTrue(math.isfinite(response.json()["workingLeadTimeMinutes"]))

    def test_metadata_returned(self):
        response = self.client.post(
            "/predict",
            json={"featureSchemaVersion": "1", "features": _valid_features()},
        )
        body = response.json()
        for key in ("modelVersion", "featureSchemaVersion", "trainingDatasetVersion"):
            self.assertIn(key, body)

    def test_metadata_version_returned_matches_artifact(self):
        response = self.client.post(
            "/predict",
            json={"featureSchemaVersion": "1", "features": _valid_features()},
        )
        self.assertEqual(response.json()["modelVersion"], "xgb-v0.1")

    def test_missing_required_model_feature(self):
        features = _valid_features()
        del features["bom_item_count"]
        response = self.client.post(
            "/predict",
            json={"featureSchemaVersion": "1", "features": features},
        )
        self.assertEqual(response.status_code, 422)

    def test_unused_sad_feature_missing_or_null_does_not_reject(self):
        # SAD §9.9 features outside the loaded model's feature_order (e.g.
        # missing_material_count) must not be required.
        features = _valid_features()
        features["missing_material_count"] = None
        features["work_center_load_ratio"] = None
        response = self.client.post(
            "/predict",
            json={"featureSchemaVersion": "1", "features": features},
        )
        self.assertEqual(response.status_code, 200)

    def test_invalid_numeric_feature_type(self):
        features = _valid_features()
        features["quantity"] = "five"
        response = self.client.post(
            "/predict",
            json={"featureSchemaVersion": "1", "features": features},
        )
        self.assertEqual(response.status_code, 422)

    def test_nan_input_rejected(self):
        raw_body = '{"featureSchemaVersion": "1", "features": {"product_ref": "P001", "quantity": NaN, "bom_item_count": 3}}'
        response = self.client.post(
            "/predict",
            content=raw_body,
            headers={"content-type": "application/json"},
        )
        self.assertEqual(response.status_code, 422)

    def test_infinity_input_rejected(self):
        raw_body = '{"featureSchemaVersion": "1", "features": {"product_ref": "P001", "quantity": Infinity, "bom_item_count": 3}}'
        response = self.client.post(
            "/predict",
            content=raw_body,
            headers={"content-type": "application/json"},
        )
        self.assertEqual(response.status_code, 422)

    def test_schema_match(self):
        response = self.client.post(
            "/predict",
            json={"featureSchemaVersion": str(CANONICAL_METADATA["feature_schema_version"]), "features": _valid_features()},
        )
        self.assertEqual(response.status_code, 200)

    def test_schema_mismatch(self):
        response = self.client.post(
            "/predict",
            json={"featureSchemaVersion": "999", "features": _valid_features()},
        )
        self.assertEqual(response.status_code, 422)

    def test_known_product_ref_encoding(self):
        response = self.client.post(
            "/predict",
            json={"featureSchemaVersion": "1", "features": _valid_features()},
        )
        self.assertEqual(response.status_code, 200)

    def test_unknown_product_ref_encoding(self):
        features = _valid_features()
        features["product_ref"] = "P999-UNKNOWN"
        response = self.client.post(
            "/predict",
            json={"featureSchemaVersion": "1", "features": features},
        )
        # Unknown category maps to unknown_category_code — not rejected.
        self.assertEqual(response.status_code, 200)

    def test_model_loaded_once(self):
        calls = []
        original = loader.load_artifact
        try:
            def counting_load_artifact(*args, **kwargs):
                calls.append(1)
                return original(*args, **kwargs)

            loader.load_artifact = counting_load_artifact
            main.state = main.ServiceState()
            with TestClient(main.app) as client:
                client.get("/health")
                client.get("/health")
                client.post("/predict", json={"featureSchemaVersion": "1", "features": _valid_features()})
        finally:
            loader.load_artifact = original
        self.assertEqual(len(calls), 1)


class HealthModelMissingTests(unittest.TestCase):
    """Simulates a load failure by making load_artifact raise during the
    app's real startup lifespan — not by hand-crafting ServiceState, so the
    controlled model-unavailable path is exercised end to end."""

    def setUp(self):
        self._original_load_artifact = loader.load_artifact
        loader.load_artifact = self._raise_artifact_error
        main.state = main.ServiceState()
        self.client = TestClient(main.app)
        self.client.__enter__()

    def tearDown(self):
        self.client.__exit__(None, None, None)
        loader.load_artifact = self._original_load_artifact

    @staticmethod
    def _raise_artifact_error(*args, **kwargs):
        raise loader.ModelArtifactError("simulated load failure")

    def test_health_model_missing_returns_503(self):
        response = self.client.get("/health")
        self.assertEqual(response.status_code, 503)
        self.assertEqual(response.json(), {"status": "model_unavailable"})

    def test_predict_model_missing_returns_503(self):
        response = self.client.post(
            "/predict",
            json={"featureSchemaVersion": "1", "features": _valid_features()},
        )
        self.assertEqual(response.status_code, 503)


class FeaturePreparationTests(unittest.TestCase):
    def test_feature_order_from_metadata(self):
        metadata = {
            "feature_order": ["bom_item_count", "product_ref", "quantity"],
            "categorical_encodings": {"product_ref": {"P001": 7}},
            "unknown_category_code": -1,
        }
        vector = prepare_feature_vector(
            {"product_ref": "P001", "quantity": 5, "bom_item_count": 3}, metadata
        )
        self.assertEqual(vector, [3.0, 7.0, 5.0])

    def test_known_category_uses_encoding_table(self):
        metadata = {
            "feature_order": ["product_ref"],
            "categorical_encodings": {"product_ref": {"P001": 0, "P002": 1}},
            "unknown_category_code": -1,
        }
        self.assertEqual(prepare_feature_vector({"product_ref": "P002"}, metadata), [1.0])

    def test_unknown_category_uses_unknown_code(self):
        metadata = {
            "feature_order": ["product_ref"],
            "categorical_encodings": {"product_ref": {"P001": 0}},
            "unknown_category_code": -1,
        }
        self.assertEqual(prepare_feature_vector({"product_ref": "ZZZZ"}, metadata), [-1.0])

    def test_missing_feature_raises(self):
        metadata = {"feature_order": ["quantity"], "categorical_encodings": {}, "unknown_category_code": -1}
        with self.assertRaises(FeatureValidationError):
            prepare_feature_vector({}, metadata)

    def test_null_feature_raises(self):
        metadata = {"feature_order": ["quantity"], "categorical_encodings": {}, "unknown_category_code": -1}
        with self.assertRaises(FeatureValidationError):
            prepare_feature_vector({"quantity": None}, metadata)

    def test_non_numeric_feature_raises(self):
        metadata = {"feature_order": ["quantity"], "categorical_encodings": {}, "unknown_category_code": -1}
        with self.assertRaises(FeatureValidationError):
            prepare_feature_vector({"quantity": "not-a-number"}, metadata)

    def test_nan_feature_raises(self):
        metadata = {"feature_order": ["quantity"], "categorical_encodings": {}, "unknown_category_code": -1}
        with self.assertRaises(FeatureValidationError):
            prepare_feature_vector({"quantity": float("nan")}, metadata)

    def test_infinite_feature_raises(self):
        metadata = {"feature_order": ["quantity"], "categorical_encodings": {}, "unknown_category_code": -1}
        with self.assertRaises(FeatureValidationError):
            prepare_feature_vector({"quantity": float("inf")}, metadata)

    def test_extra_unused_features_ignored(self):
        metadata = {"feature_order": ["quantity"], "categorical_encodings": {}, "unknown_category_code": -1}
        vector = prepare_feature_vector({"quantity": 5, "shift_capacity_minutes": None}, metadata)
        self.assertEqual(vector, [5.0])


class ArtifactLoadingTests(unittest.TestCase):
    def test_artifact_missing_raises(self):
        with tempfile.TemporaryDirectory() as tmp:
            with self.assertRaises(loader.ModelArtifactError):
                loader.load_artifact(Path(tmp) / "does-not-exist")

    def test_artifact_invalid_model_file_raises(self):
        with tempfile.TemporaryDirectory() as tmp:
            tmp_path = Path(tmp)
            (tmp_path / "model.json").write_text("not valid xgboost json", encoding="utf-8")
            (tmp_path / "model-metadata.json").write_text(
                json.dumps(
                    {
                        "feature_order": ["quantity"],
                        "categorical_encodings": {},
                        "unknown_category_code": -1,
                        "feature_schema_version": 1,
                        "model_version": "xgb-test",
                        "training_dataset_version": "synthetic-v1",
                    }
                ),
                encoding="utf-8",
            )
            with self.assertRaises(loader.ModelArtifactError):
                loader.load_artifact(tmp_path)

    def test_artifact_metadata_missing_required_field_raises(self):
        with tempfile.TemporaryDirectory() as tmp:
            tmp_path = Path(tmp)
            (tmp_path / "model.json").write_text("{}", encoding="utf-8")
            (tmp_path / "model-metadata.json").write_text(
                json.dumps({"feature_order": ["quantity"]}), encoding="utf-8"
            )
            with self.assertRaises(loader.ModelArtifactError):
                loader.load_artifact(tmp_path)

    def test_canonical_artifact_loads_successfully(self):
        artifact = loader.load_artifact()
        self.assertEqual(artifact.metadata["model_version"], "xgb-v0.1")
        prediction = float(artifact.model.predict([[0, 5, 3]])[0])
        self.assertTrue(math.isfinite(prediction))


class RuntimeTrainingRegressionTests(unittest.TestCase):
    def test_service_modules_never_call_fit(self):
        for module in (main, loader):
            source = inspect.getsource(module)
            self.assertNotIn(".fit(", source)
            self.assertNotIn(".partial_fit(", source)


if __name__ == "__main__":
    unittest.main()
