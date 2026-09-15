namespace App.Application.Prediction;

public enum FinalPredictionStatus { HybridCalculated = 1, RuleBasedFallback = 2, AiOnlyCandidate = 3, InsufficientData = 4, Infeasible = 5 }
public enum PredictionFallbackReason
{
    None = 0, AiPredictionTimeout, AiPredictionOutsideTolerance, AiServiceUnavailable,
    InvalidAiResponse, InsufficientAiFeatures, AiModelUnavailable, AiModelVersionMismatch,
    AiFeatureSchemaMismatch, InvalidAiValue, RuleBasedEngineError, InvalidErpData,
    OperationGraphCycleDetected
}

public sealed record FinalPredictionResult(
    FinalPredictionStatus Status,
    PredictionFallbackReason FallbackReason,
    long? WorkingLeadTimeMinutes,
    string? CombinationStrategy,
    decimal? RuleBasedWeight,
    decimal? AiWeight,
    long? AbsoluteDifferenceMinutes,
    decimal? RelativeDifferencePercent,
    DateTimeOffset? EstimatedStart = null,
    DateTimeOffset? EstimatedEnd = null,
    DateTimeOffset? EstimatedDelivery = null);

public sealed record PredictionAggregateResult(
    string OrderReference,
    PredictionProviderResult RuleBasedPrediction,
    PredictionProviderResult AiPrediction,
    FinalPredictionResult FinalPrediction);
