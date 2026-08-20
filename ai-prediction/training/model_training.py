"""XGBoost working-lead-time model training (T-907).

Prepared Dataset (T-905, unmodified)
  -> Contract Validation + Feature Extraction + Preprocessing (prepare_dataset.prepare)
  -> Baseline Evaluation (MeanBaselineRegressor)
  -> XGBoost Fit
  -> Prediction
  -> Evaluation (MAE / RMSE / R^2)
  -> Artifact Save
  -> Artifact Reload Verification

This module trains OFFLINE only. It must never be imported or called from a
FastAPI request path — there is no fit/partial_fit/retraining at inference
time (SAD §9.11: the AI service is a fixed, pre-trained artifact).

Target: T-905 canonical target (`actual_total_working_lead_time_minutes`),
via prepare_dataset.prepare() — never recomputed from dates here. Feature
set: exactly T-905's resolved AVAILABLE features (product_ref, quantity,
bom_item_count) — this module does not add features of its own.

Artifact lifecycle (T-907 decision): this module's --out is an EPHEMERAL
staging directory (default: training/artifacts/<model_version>/, git-
ignored) — it is generated training-run output, not the runtime source of
truth by itself. The single canonical, version-controlled artifact location
FastAPI/T-909 loads from is ai-prediction/model/artifacts/<model_version>/,
populated only by the explicit promote_artifact.py step — never written to
directly by this module, to avoid two ambiguous sources of truth.
"""

from __future__ import annotations

import argparse
import json
import math
from datetime import datetime, timezone
from pathlib import Path

from xgboost import XGBRegressor

from training.dataset_contract import CANONICAL_TARGET_FIELD
from training.prepare_dataset import DEFAULT_GROUND_TRUTH, DEFAULT_SEED, prepare

MODEL_VERSION = "xgb-v0.1"
MODEL_TYPE = "XGBRegressor"
BASELINE_MODEL_TYPE = "MeanBaselineRegressor"

# Fixed, non-tuned hyperparameters (Optuna/GridSearch are explicitly out of
# T-907's scope). random_state + n_jobs=1 for reproducible tree building.
XGBOOST_RANDOM_STATE = 42
XGBOOST_PARAMS = {
    "n_estimators": 200,
    "max_depth": 4,
    "learning_rate": 0.1,
    "random_state": XGBOOST_RANDOM_STATE,
    "n_jobs": 1,
    "objective": "reg:squarederror",
}


class DatasetTooSmallError(Exception):
    """Raised when the prepared train or test split is empty."""


class MeanBaselineRegressor:
    """Predicts the training-set target mean for every row.

    Not a production artifact — exists only to answer "does XGBoost actually
    learn something beyond a constant guess?" (see evaluate_models()).
    """

    def __init__(self) -> None:
        self._mean: float | None = None

    def fit(self, y_train: list[float]) -> "MeanBaselineRegressor":
        self._mean = sum(y_train) / len(y_train)
        return self

    def predict(self, count: int) -> list[float]:
        if self._mean is None:
            raise RuntimeError("MeanBaselineRegressor.predict() called before fit().")
        return [self._mean] * count


def _rows_to_xy(rows: list[dict]) -> tuple[list[list[float]], list[float]]:
    return [row["x"] for row in rows], [row["y"] for row in rows]


def mean_absolute_error(y_true: list[float], y_pred: list[float]) -> float:
    return sum(abs(a - b) for a, b in zip(y_true, y_pred)) / len(y_true)


def root_mean_squared_error(y_true: list[float], y_pred: list[float]) -> float:
    return math.sqrt(sum((a - b) ** 2 for a, b in zip(y_true, y_pred)) / len(y_true))


def r_squared(y_true: list[float], y_pred: list[float]) -> float:
    mean_y = sum(y_true) / len(y_true)
    ss_res = sum((a - b) ** 2 for a, b in zip(y_true, y_pred))
    ss_tot = sum((a - mean_y) ** 2 for a in y_true)
    if ss_tot == 0:
        return float("nan")
    return 1 - ss_res / ss_tot


def evaluate(y_true: list[float], y_pred: list[float]) -> dict:
    return {
        "mae": mean_absolute_error(y_true, y_pred),
        "rmse": root_mean_squared_error(y_true, y_pred),
        "r2": r_squared(y_true, y_pred),
    }


def train(ground_truth_path: Path, seed_path: Path) -> dict:
    """Runs the full offline training pipeline and returns in-memory results
    (model, metadata, evaluation report) — does not write to disk.
    """
    prepared = prepare(ground_truth_path, seed_path)  # T-905, unmodified
    dataset_metadata = prepared["metadata"]
    train_rows, test_rows = prepared["train"], prepared["test"]

    if not train_rows or not test_rows:
        raise DatasetTooSmallError(
            f"Prepared dataset split is too small to train/evaluate "
            f"(train={len(train_rows)}, test={len(test_rows)})."
        )

    X_train, y_train = _rows_to_xy(train_rows)
    X_test, y_test = _rows_to_xy(test_rows)

    baseline = MeanBaselineRegressor().fit(y_train)
    baseline_metrics = evaluate(y_test, baseline.predict(len(y_test)))

    model = XGBRegressor(**XGBOOST_PARAMS)
    model.fit(X_train, y_train)
    xgb_pred = [float(v) for v in model.predict(X_test)]
    xgb_metrics = evaluate(y_test, xgb_pred)

    evaluation_report = {
        "baseline": {"model_type": BASELINE_MODEL_TYPE, **baseline_metrics},
        "xgboost": {"model_type": MODEL_TYPE, **xgb_metrics},
    }

    model_metadata = {
        "model_version": MODEL_VERSION,
        "model_type": MODEL_TYPE,
        "feature_schema_version": dataset_metadata["feature_schema_version"],
        "training_dataset_version": dataset_metadata["training_dataset_version"],
        "target_name": CANONICAL_TARGET_FIELD,
        "feature_order": dataset_metadata["feature_order"],
        "categorical_encodings": dataset_metadata["categorical_encodings"],
        "unknown_category_code": dataset_metadata["unknown_category_code"],
        "training_timestamp_utc": datetime.now(timezone.utc).isoformat(),
        "split": dataset_metadata["split"],
        "train_row_count": len(train_rows),
        "test_row_count": len(test_rows),
        "xgboost_params": XGBOOST_PARAMS,
        "dataset_source": dataset_metadata["source"],
        "target_mapping_warnings": dataset_metadata["target_mapping_warnings"],
        "metrics": evaluation_report,
    }

    return {
        "model": model,
        "model_metadata": model_metadata,
        "evaluation": evaluation_report,
        "X_test_sample": X_test[0] if X_test else None,
    }


def save_artifacts(result: dict, out_dir: Path) -> None:
    out_dir.mkdir(parents=True, exist_ok=True)
    result["model"].save_model(str(out_dir / "model.json"))
    (out_dir / "model-metadata.json").write_text(
        json.dumps(result["model_metadata"], indent=2, ensure_ascii=False), encoding="utf-8"
    )
    (out_dir / "evaluation.json").write_text(
        json.dumps(result["evaluation"], indent=2, ensure_ascii=False), encoding="utf-8"
    )


def load_model(model_path: Path) -> XGBRegressor:
    model = XGBRegressor()
    model.load_model(str(model_path))
    return model


def main() -> None:
    parser = argparse.ArgumentParser(description="Train the XGBoost working-lead-time model (T-907).")
    parser.add_argument("--ground-truth", type=Path, default=DEFAULT_GROUND_TRUTH)
    parser.add_argument("--seed", type=Path, default=DEFAULT_SEED)
    parser.add_argument(
        "--out",
        type=Path,
        default=Path(__file__).resolve().parent / "artifacts" / MODEL_VERSION,
    )
    args = parser.parse_args()

    result = train(args.ground_truth, args.seed)
    save_artifacts(result, args.out)

    print(json.dumps(result["evaluation"], indent=2, ensure_ascii=False))
    print(f"Artifacts written to {args.out}")

    # Artifact reload verification (DoD requirement) — fail loudly if the
    # freshly saved model cannot be reloaded and used for prediction.
    reloaded = load_model(args.out / "model.json")
    sample = result["X_test_sample"]
    if sample is not None:
        prediction = float(reloaded.predict([sample])[0])
        if not math.isfinite(prediction):
            raise RuntimeError(f"Reloaded model produced a non-finite prediction: {prediction!r}")
        print(f"Artifact reload verified: predict({sample}) = {prediction}")


if __name__ == "__main__":
    main()
