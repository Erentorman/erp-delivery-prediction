namespace App.Application.Prediction;

public interface IPredictionRepository
{
    Task SaveAsync(PredictionAggregateResult result, CancellationToken cancellationToken = default);
}
