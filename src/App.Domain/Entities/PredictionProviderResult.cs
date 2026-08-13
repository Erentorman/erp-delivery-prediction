namespace App.Domain.Entities;

public class PredictionProviderResult
{
    public long Id { get; set; }
    public long PredictionResultId { get; set; }
    public string ProviderType { get; set; } = string.Empty;
    public string ProviderStatus { get; set; } = string.Empty;
    public long? WorkingLeadTimeMinutes { get; set; }
    public DateTime? EstimatedDeliveryDate { get; set; }

    // Only populated for the (not-yet-implemented) Ai provider row.
    public string? ModelVersion { get; set; }
    public string? FeatureSchemaVersion { get; set; }
    public string? TrainingDatasetVersion { get; set; }
    public string? FeaturePayload { get; set; }

    public string? Warnings { get; set; }
    public DateTime CreatedAt { get; set; }

    public PredictionResult PredictionResult { get; set; } = null!;
}
