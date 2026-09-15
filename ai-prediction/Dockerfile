# ------------------------------------------------------------
# ai-prediction — Dockerfile (T-909)
#
# Runtime endpoints (SAD §9.11):
#   GET  /health   — healthy once the canonical model artifact is loaded
#   POST /predict  — working lead time inference from the loaded artifact
#
# Canonical runtime model source of truth (committed, version-controlled):
#   model/artifacts/xgb-v0.1/{model.json,model-metadata.json,evaluation.json}
# Training code (training/) and its ephemeral staging artifacts
# (training/artifacts/) are excluded from the image via .dockerignore —
# this image never fits/retrains a model, it only loads the committed one.
# ------------------------------------------------------------
FROM python:3.14-slim
WORKDIR /app

COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

COPY . .

ENV AI_MODEL_ARTIFACT_DIR=/app/model/artifacts/xgb-v0.1

EXPOSE 8000
CMD ["uvicorn", "main:app", "--host", "0.0.0.0", "--port", "8000"]
