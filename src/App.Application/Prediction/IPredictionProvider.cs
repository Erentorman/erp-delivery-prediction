using App.Domain.Prediction;

namespace App.Application.Prediction;

public interface IPredictionProvider
{
    PredictionProviderType ProviderType { get; }
    Task<PredictionProviderResult> PredictAsync(PredictionContext context, CancellationToken cancellationToken = default);
}
