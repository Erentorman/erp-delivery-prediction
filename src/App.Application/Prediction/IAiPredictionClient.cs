using App.Application.Contracts.Prediction;

namespace App.Application.Prediction;

public interface IAiPredictionClient
{
    Task<AiPredictionResult> GetPredictionAsync(
        AiPredictionRequest request,
        CancellationToken cancellationToken = default);
}
