namespace App.Application.Prediction;

public interface IPredictionRepository
{
    Task SaveAsync(PredictionPersistenceRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PredictionHistoryListItem>> GetHistoryAsync(
        string? orderReference,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<PredictionHistoryDetail?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
}
