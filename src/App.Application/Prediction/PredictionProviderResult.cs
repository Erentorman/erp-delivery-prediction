using App.Application.Contracts.Prediction;

namespace App.Application.Prediction;

public enum PredictionProviderType { RuleBased = 1, Ai = 2 }

public sealed record PredictionProviderResult(
    PredictionProviderType ProviderType,
    AiProviderStatus Status,
    decimal? WorkingLeadTimeMinutes = null,
    RuleBasedPredictionResult? RuleBasedPrediction = null,
    string? ModelVersion = null,
    string? FeatureSchemaVersion = null,
    string? TrainingDatasetVersion = null,
    AiFeaturePayload? FeaturePayload = null,
    IReadOnlyList<string>? Warnings = null,
    long DurationMs = 0,
    string? FailureCode = null);
