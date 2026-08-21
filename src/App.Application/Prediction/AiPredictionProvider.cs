using App.Application.Contracts.Prediction;
using App.Domain.Prediction;

namespace App.Application.Prediction;

public sealed class AiPredictionProvider
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

    public Task<AiPredictionResult> PredictAsync(
        PredictionContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var features = _featureBuilder.Build(context);
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
