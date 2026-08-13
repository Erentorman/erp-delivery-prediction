namespace App.Application.Prediction;

public sealed record PredictionPersistenceRequest(
    string? ErpOrderRef,
    bool IsSimulation,
    WhatIfSimulationInputSummary? SimulationInput,
    DateTimeOffset? RequestedDeliveryDate,
    RuleBasedPredictionResult Result);
