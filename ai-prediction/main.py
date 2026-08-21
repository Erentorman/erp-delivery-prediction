"""FastAPI AI Prediction inference service (T-909).

Loads the canonical, version-controlled `xgb-v0.1` artifact
(model/artifacts/<model_version>/) once at startup and serves /predict from
it. This service never fits/retrains a model at request time (SAD §9.11:
the AI service is a fixed, pre-trained artifact) and never depends on
Rule-Based/CPM intermediate results — it only returns
`workingLeadTimeMinutes`; date/hybrid logic stays in the .NET side.
"""

from __future__ import annotations

import math
from contextlib import asynccontextmanager
from typing import Any

from fastapi import FastAPI
from fastapi.responses import JSONResponse
from pydantic import BaseModel, Field

from model import loader
from model.loader import ModelArtifactError
from preprocessing.features import FeatureValidationError, prepare_feature_vector


class ServiceState:
    """Holds the loaded artifact (or the load failure) for the app's lifetime."""

    def __init__(self) -> None:
        self.model = None
        self.metadata: dict | None = None
        self.load_error: str | None = None

    @property
    def is_ready(self) -> bool:
        return self.model is not None and self.metadata is not None


state = ServiceState()


def _load_model_once() -> None:
    try:
        artifact = loader.load_artifact()
    except ModelArtifactError:
        state.model = None
        state.metadata = None
        state.load_error = "model_unavailable"
        return
    state.model = artifact.model
    state.metadata = artifact.metadata
    state.load_error = None


@asynccontextmanager
async def lifespan(_: FastAPI):
    _load_model_once()
    yield


app = FastAPI(title="AI Prediction Service", lifespan=lifespan)


class PredictRequest(BaseModel):
    featureSchemaVersion: str
    features: dict[str, Any] = Field(default_factory=dict)


class PredictResponse(BaseModel):
    workingLeadTimeMinutes: float
    modelVersion: str
    featureSchemaVersion: str
    trainingDatasetVersion: str


@app.get("/health")
def health_check():
    if not state.is_ready:
        return JSONResponse(status_code=503, content={"status": "model_unavailable"})
    return {"status": "healthy"}


@app.post("/predict", response_model=PredictResponse)
def predict(payload: PredictRequest):
    if not state.is_ready:
        return JSONResponse(status_code=503, content={"detail": "model_unavailable"})

    metadata = state.metadata
    expected_schema_version = str(metadata["feature_schema_version"])
    if payload.featureSchemaVersion != expected_schema_version:
        return JSONResponse(
            status_code=422,
            content={
                "detail": (
                    f"featureSchemaVersion mismatch: expected "
                    f"'{expected_schema_version}', got '{payload.featureSchemaVersion}'."
                )
            },
        )

    try:
        feature_vector = prepare_feature_vector(payload.features, metadata)
    except FeatureValidationError as exc:
        return JSONResponse(status_code=422, content={"detail": str(exc)})

    prediction = float(state.model.predict([feature_vector])[0])
    if not math.isfinite(prediction):
        return JSONResponse(status_code=503, content={"detail": "model_unavailable"})

    return PredictResponse(
        workingLeadTimeMinutes=prediction,
        modelVersion=str(metadata["model_version"]),
        featureSchemaVersion=expected_schema_version,
        trainingDatasetVersion=str(metadata["training_dataset_version"]),
    )
