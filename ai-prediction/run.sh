#!/bin/bash
# Local run command for AI Prediction Service
# Ensure you have installed dependencies via pip install -r requirements.txt
uvicorn main:app --host 0.0.0.0 --port 8000 --reload
