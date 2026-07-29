# ------------------------------------------------------------
# ai-prediction — Dockerfile (T-104 skeleton, wired to T-105 sources)
#
# T-105 has provided:
#   main.py              — FastAPI app with /health endpoint
#   requirements.txt     — fastapi>=0.111.0, uvicorn>=0.30.1
#   model/__init__.py    — model package stub
#   preprocessing/__init__.py — preprocessing package stub
#
# TODO (T-105 in-progress):
#   POST /predict endpoint — accepts feature payload,
#                            returns working_lead_time_minutes
#   Model artifact         — /app/models/model.pkl
#
# Required endpoints (SAD §9.11):
#   GET  /health   — live (returns {"status": "healthy"})
#   POST /predict  — pending T-105 completion
# ------------------------------------------------------------
FROM python:3.14-slim
WORKDIR /app

COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

COPY . .

EXPOSE 8000
CMD ["uvicorn", "main:app", "--host", "0.0.0.0", "--port", "8000"]
