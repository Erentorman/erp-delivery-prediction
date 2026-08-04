from fastapi import FastAPI

app = FastAPI(title="AI Prediction Service")

@app.get("/health")
def health_check():
    return {"status": "healthy"}
