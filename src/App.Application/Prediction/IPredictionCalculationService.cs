using App.Application.Common;
using App.Application.Contracts.Prediction;

namespace App.Application.Prediction;

public interface IPredictionCalculationService
{
    Task<Result<PredictionResponse>> CalculateAsync(string orderReference, CancellationToken cancellationToken = default);
}
