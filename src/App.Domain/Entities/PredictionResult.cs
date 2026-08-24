namespace App.Domain.Entities;

public sealed class PredictionResult
{
    public long Id { get; set; }
    public string ErpOrderReference { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string FinalStatus { get; set; } = string.Empty;
    public string FallbackReason { get; set; } = string.Empty;
    public string? CombinationStrategy { get; set; }
    public decimal? RuleBasedWeight { get; set; }
    public decimal? AiWeight { get; set; }
    public long? FinalWorkingLeadTimeMinutes { get; set; }
    public long? AbsoluteDifferenceMinutes { get; set; }
    public decimal? RelativeDifferencePercent { get; set; }
    public DateTimeOffset? ProductionStart { get; set; }
    public DateTimeOffset? ProductionEnd { get; set; }
    public DateTimeOffset? DeliveryDate { get; set; }
    public DateTimeOffset CalculatedAt { get; set; }
    public ICollection<PredictionProviderResultEntity> ProviderResults { get; set; } = new List<PredictionProviderResultEntity>();
}

public sealed class PredictionProviderResultEntity
{
    public long Id { get; set; }
    public long PredictionResultId { get; set; }
    public PredictionResult PredictionResult { get; set; } = null!;
    public string ProviderType { get; set; } = string.Empty;
    public string ProviderStatus { get; set; } = string.Empty;
    public long? WorkingLeadTimeMinutes { get; set; }
    public DateTimeOffset? EstimatedDeliveryDate { get; set; }
    public string? ModelVersion { get; set; }
    public string? FeatureSchemaVersion { get; set; }
    public string? TrainingDatasetVersion { get; set; }
    public string? FeaturePayload { get; set; }
    public string? Warnings { get; set; }
    public int DurationMs { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
