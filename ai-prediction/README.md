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
