# AI Prediction Service

This is the FastAPI-based Python service for the AI baseline prediction, as described in SAD §6.4 and §7.

## Setup

It is recommended to use a virtual environment for dependency management.

```bash
# Create a virtual environment
python -m venv venv

# Activate the virtual environment
# On Linux/macOS:
source venv/bin/activate
# On Windows:
# venv\Scripts\activate

# Install dependencies
pip install -r requirements.txt
```

## Running Locally

To run the FastAPI application locally, execute:

```bash
./run.sh
```
Or directly with uvicorn:
```bash
uvicorn main:app --host 0.0.0.0 --port 8000 --reload
```

## Endpoints

- `GET /health` : Health check endpoint.

## Model Training (offline, T-905/T-907)

Dataset preparation and model training are **offline-only — never triggered
by a FastAPI request**. There is no fit/partial_fit/retraining at inference
time (SAD §9.11: the AI service is a fixed, pre-trained artifact). Training
uses its own dependency set (`requirements-training.txt`), not installed
into the runtime image (`Dockerfile` installs `requirements.txt` only).

```bash
pip install -r requirements.txt -r requirements-training.txt

# Dataset preparation only (T-905) — writes training/output/ (git-ignored)
python -m training.prepare_dataset

# Full training pipeline (T-905 dataset prep + T-907 XGBoost training).
# Writes to an EPHEMERAL staging directory (default:
# training/artifacts/<model_version>/, git-ignored) — this is generated
# training-run output, not the runtime artifact by itself.
python -m training.model_training
```

Both default to the small committed preview fixture
(`tests/App.Integration.Tests/Fixtures/ErpSeedConverterPreview/`). To train
against a full dataset, pass `--ground-truth` / `--seed` at a
`prediction-ground-truth.json` / `mock-erp-seed.json` pair produced by
`tools/erp-seed-converter/convert.py` — see
`ai-prediction/docs/training-dataset-contract.md` for the dataset contract
(canonical target, leakage rules, feature availability) this pipeline
enforces.

### Runtime model artifact

The **canonical, version-controlled runtime artifact** — the one the
FastAPI service actually loads — lives at:

```
ai-prediction/model/artifacts/<model_version>/
    model.json           # XGBoost native format (XGBRegressor.save_model)
    model-metadata.json  # versions, feature contract, encodings, metrics, dataset provenance
    evaluation.json       # baseline vs. XGBoost metrics (MAE / RMSE / R²)
```

This directory **is committed to the repository** (small, ~350 KB — no Git
LFS needed) and is copied into the Docker image by the existing
`COPY . .` in `Dockerfile` (no `.dockerignore` excludes it) — no Dockerfile
change was required to make this reachable at build time.

A staged, freshly trained artifact is promoted to this canonical location
with an explicit, separate step — training never writes here directly, so
there is exactly one source of truth for "the" runtime model:

```bash
python -m training.promote_artifact --source training/artifacts/xgb-v0.1
```

**The runtime service (T-909) loads this artifact — it does not train or
retrain.** Model updates happen by running training + promotion offline and
committing the new/updated `ai-prediction/model/artifacts/<model_version>/`
directory, not by the running service.
