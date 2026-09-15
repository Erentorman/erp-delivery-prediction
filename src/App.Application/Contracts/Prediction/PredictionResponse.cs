using App.Application.Prediction;

namespace App.Application.Contracts.Prediction;

public sealed record ProviderPredictionResponse(
    string ProviderType, string Status, decimal? WorkingLeadTimeMinutes,
    string? ModelVersion = null, string? FeatureSchemaVersion = null,
    string? TrainingDatasetVersion = null, IReadOnlyList<string>? Warnings = null,
    long DurationMs = 0);

public sealed record FinalPredictionResponse(
    string Status, string FallbackReason, long? WorkingLeadTimeMinutes,
    DateTimeOffset? EstimatedStart, DateTimeOffset? EstimatedEnd, DateTimeOffset? EstimatedDelivery,
    string? CombinationStrategy, decimal? RuleBasedWeight, decimal? AiWeight,
    long? AbsoluteDifferenceMinutes, decimal? RelativeDifferencePercent);

public sealed record PredictionResponse(
    string OrderReference,
    DateTimeOffset? EstimatedStart,
    DateTimeOffset? EstimatedEnd,
    DateTimeOffset? EstimatedDelivery,
    IReadOnlyList<string> CriticalPathOperations,
    IReadOnlyList<string> AppliedFallbackReasons,
    IReadOnlyList<MaterialShortage> Shortages,
    IReadOnlyList<TimelineItem> Timeline,
    ProviderPredictionResponse RuleBasedPrediction,
    ProviderPredictionResponse AiPrediction,
    FinalPredictionResponse FinalPrediction)
{
    public static PredictionResponse From(PredictionAggregateResult aggregate)
    {
        var rb = aggregate.RuleBasedPrediction;
        var ai = aggregate.AiPrediction;
        var legacy = rb.RuleBasedPrediction;
        var final = aggregate.FinalPrediction;
        return new PredictionResponse(aggregate.OrderReference, final.EstimatedStart, final.EstimatedEnd,
            final.EstimatedDelivery, legacy?.CriticalPathOperations ?? [], legacy?.AppliedFallbackReasons ?? [],
            legacy?.Shortages ?? [], legacy?.Timeline ?? [], Map(rb), Map(ai),
            new FinalPredictionResponse(final.Status.ToString(), final.FallbackReason.ToString(),
                final.WorkingLeadTimeMinutes, final.EstimatedStart, final.EstimatedEnd, final.EstimatedDelivery,
                final.CombinationStrategy, final.RuleBasedWeight, final.AiWeight,
                final.AbsoluteDifferenceMinutes, final.RelativeDifferencePercent));
    }

    private static ProviderPredictionResponse Map(PredictionProviderResult value) =>
        new(value.ProviderType.ToString(), value.Status.ToString(), value.WorkingLeadTimeMinutes,
            value.ModelVersion, value.FeatureSchemaVersion, value.TrainingDatasetVersion,
            value.Warnings, value.DurationMs);
}
