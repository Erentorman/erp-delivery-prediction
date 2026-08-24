using System.Text.Json;
using App.Application.Prediction;
using App.Domain.Entities;

namespace App.Persistence.Prediction;

public sealed class PredictionRepository : IPredictionRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly AppDbContext _context;
    public PredictionRepository(AppDbContext context) => _context = context;

    public async Task SaveAsync(PredictionAggregateResult result, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(result);
        var final = result.FinalPrediction;
        var entity = new PredictionResult
        {
            ErpOrderReference = result.OrderReference,
            Status = final.Status is FinalPredictionStatus.HybridCalculated or FinalPredictionStatus.RuleBasedFallback ? "Calculated" : "InsufficientData",
            FinalStatus = final.Status.ToString(), FallbackReason = final.FallbackReason.ToString(),
            CombinationStrategy = final.CombinationStrategy, RuleBasedWeight = final.RuleBasedWeight,
            AiWeight = final.AiWeight, FinalWorkingLeadTimeMinutes = final.WorkingLeadTimeMinutes,
            AbsoluteDifferenceMinutes = final.AbsoluteDifferenceMinutes,
            RelativeDifferencePercent = final.RelativeDifferencePercent,
            ProductionStart = final.EstimatedStart, ProductionEnd = final.EstimatedEnd,
            DeliveryDate = final.EstimatedDelivery, CalculatedAt = DateTimeOffset.UtcNow
        };
        entity.ProviderResults.Add(Map(result.RuleBasedPrediction, entity));
        entity.ProviderResults.Add(Map(result.AiPrediction, entity));
        _context.PredictionResults.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private static PredictionProviderResultEntity Map(PredictionProviderResult value, PredictionResult owner) => new()
    {
        PredictionResult = owner, ProviderType = value.ProviderType.ToString(), ProviderStatus = value.Status.ToString(),
        WorkingLeadTimeMinutes = value.WorkingLeadTimeMinutes is decimal minutes
            ? checked((long)Math.Round(minutes, MidpointRounding.AwayFromZero)) : null,
        EstimatedDeliveryDate = value.RuleBasedPrediction?.EstimatedDelivery,
        ModelVersion = value.ModelVersion, FeatureSchemaVersion = value.FeatureSchemaVersion,
        TrainingDatasetVersion = value.TrainingDatasetVersion,
        FeaturePayload = value.ProviderType == PredictionProviderType.Ai && value.FeaturePayload is not null
            ? JsonSerializer.Serialize(value.FeaturePayload, JsonOptions) : null,
        Warnings = value.Warnings is { Count: > 0 } ? JsonSerializer.Serialize(value.Warnings, JsonOptions) : null,
        DurationMs = checked((int)Math.Min(value.DurationMs, int.MaxValue)), CreatedAt = DateTimeOffset.UtcNow
    };
}
