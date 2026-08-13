namespace App.Application.Prediction;

public sealed record WhatIfSimulationInputSummary(
    string ProductReference,
    decimal Quantity,
    string LocationReference);
