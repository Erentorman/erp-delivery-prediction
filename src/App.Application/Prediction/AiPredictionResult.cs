namespace App.Application.Prediction;

public sealed record AiPredictionResult(
    AiProviderStatus Status,
    double? WorkingLeadTimeMinutes = null,
    string? ModelVersion = null,
    string? FeatureSchemaVersion = null,
    string? TrainingDatasetVersion = null,
    string? Message = null)
{
    public static AiPredictionResult Failure(AiProviderStatus status, string message) =>
        new(status, Message: message);
}
