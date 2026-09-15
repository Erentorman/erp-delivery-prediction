using App.Application.Contracts.Prediction;
using App.Domain.Prediction;

namespace App.Application.Prediction;

public sealed class AiPredictionProvider : IPredictionProvider
{
    private const string InsufficientFeaturesMessage =
        "The AI model's required runtime features are unavailable.";

    private readonly IAiFeatureBuilder _featureBuilder;
    private readonly IAiPredictionClient _predictionClient;

    public AiPredictionProvider(
        IAiFeatureBuilder featureBuilder,
        IAiPredictionClient predictionClient)
    {
        _featureBuilder = featureBuilder ?? throw new ArgumentNullException(nameof(featureBuilder));
        _predictionClient = predictionClient ?? throw new ArgumentNullException(nameof(predictionClient));
    }

    public PredictionProviderType ProviderType => PredictionProviderType.Ai;

    async Task<PredictionProviderResult> IPredictionProvider.PredictAsync(
        PredictionContext context, CancellationToken cancellationToken)
    {
        var started = System.Diagnostics.Stopwatch.StartNew();
        var features = _featureBuilder.Build(context);
        var result = await PredictWithFeaturesAsync(features, cancellationToken);
        started.Stop();
        decimal? minutes = null;
        if (result.WorkingLeadTimeMinutes is double value && double.IsFinite(value) && value >= (double)decimal.MinValue && value <= (double)decimal.MaxValue)
            minutes = (decimal)value;

        return new PredictionProviderResult(
            ProviderType, result.Status, minutes,
            ModelVersion: result.ModelVersion,
            FeatureSchemaVersion: result.FeatureSchemaVersion,
            TrainingDatasetVersion: result.TrainingDatasetVersion,
            FeaturePayload: features,
            Warnings: result.Message is null ? Array.Empty<string>() : new[] { result.Message },
            DurationMs: started.ElapsedMilliseconds);
    }

    public Task<AiPredictionResult> PredictAsync(
        PredictionContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var features = _featureBuilder.Build(context);
        return PredictWithFeaturesAsync(features, cancellationToken);
    }

    private Task<AiPredictionResult> PredictWithFeaturesAsync(
        AiFeaturePayload features,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(features.ProductRef) ||
            features.Quantity <= 0m ||
            features.BomItemCount < 0)
        {
            return Task.FromResult(AiPredictionResult.Failure(
                AiProviderStatus.InsufficientFeatures,
                InsufficientFeaturesMessage));
        }

        return _predictionClient.GetPredictionAsync(
            new AiPredictionRequest(features),
            cancellationToken);
    }
}
