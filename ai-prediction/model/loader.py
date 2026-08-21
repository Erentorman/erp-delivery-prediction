"""Canonical model artifact loading for the FastAPI inference service (T-909).

Loads the XGBoost native `model.json` + `model-metadata.json` produced by
T-907's training pipeline and promoted by `training.promote_artifact` into
`ai-prediction/model/artifacts/<model_version>/` — the single runtime
source of truth. This module performs no fitting/retraining; it only reads
an already-trained artifact from disk.
"""

from __future__ import annotations

import json
import os
from pathlib import Path

from xgboost import XGBRegressor

MODEL_ARTIFACT_DIR_ENV_VAR = "AI_MODEL_ARTIFACT_DIR"

# Mirrors training.model_training.MODEL_VERSION — used only to locate the
# default artifact directory. The version reported to callers always comes
# from the loaded model-metadata.json, never from this constant.
DEFAULT_MODEL_VERSION = "xgb-v0.1"

AI_PREDICTION_ROOT = Path(__file__).resolve().parents[1]
DEFAULT_MODEL_ARTIFACT_DIR = AI_PREDICTION_ROOT / "model" / "artifacts" / DEFAULT_MODEL_VERSION

REQUIRED_METADATA_FIELDS = (
    "feature_order",
    "categorical_encodings",
    "unknown_category_code",
    "feature_schema_version",
    "model_version",
    "training_dataset_version",
)


class ModelArtifactError(Exception):
    """Raised when the canonical model artifact cannot be loaded or is invalid.

    Callers must not surface str(exception) filesystem detail to API responses.
    """


class LoadedModelArtifact:
    __slots__ = ("model", "metadata")

    def __init__(self, model: XGBRegressor, metadata: dict) -> None:
        self.model = model
        self.metadata = metadata


def resolve_artifact_dir(configured_dir: str | Path | None = None) -> Path:
    """Resolves the artifact directory to load from.

    Precedence: explicit `configured_dir` argument > AI_MODEL_ARTIFACT_DIR
    env var > canonical committed artifact directory. The env var lets
    Docker/runtime configuration point at a path, but that path must still
    resolve to the canonical committed artifact contents (SAD: no second
    ambiguous source of truth).
    """
    if configured_dir is not None:
        return Path(configured_dir)
    env_value = os.environ.get(MODEL_ARTIFACT_DIR_ENV_VAR)
    if env_value:
        return Path(env_value)
    return DEFAULT_MODEL_ARTIFACT_DIR


def load_artifact(artifact_dir: str | Path | None = None) -> LoadedModelArtifact:
    """Loads model.json + model-metadata.json once from artifact_dir.

    Raises ModelArtifactError on any missing file, unreadable JSON, missing
    required metadata field, or model file that XGBoost cannot parse.
    """
    resolved_dir = resolve_artifact_dir(artifact_dir)
    model_path = resolved_dir / "model.json"
    metadata_path = resolved_dir / "model-metadata.json"

    if not model_path.exists() or not metadata_path.exists():
        raise ModelArtifactError("Required model artifact file(s) not found.")

    try:
        metadata = json.loads(metadata_path.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError) as exc:
        raise ModelArtifactError("Model metadata could not be read.") from exc

    missing_fields = [field for field in REQUIRED_METADATA_FIELDS if field not in metadata]
    if missing_fields:
        raise ModelArtifactError("Model metadata is missing required field(s).")

    model = XGBRegressor()
    try:
        model.load_model(str(model_path))
    except Exception as exc:
        raise ModelArtifactError("Model artifact could not be loaded.") from exc

    return LoadedModelArtifact(model=model, metadata=metadata)
