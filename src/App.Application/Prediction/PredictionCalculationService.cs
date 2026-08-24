using App.Application.Abstractions.Erp;
using App.Application.Common;
using App.Application.Contracts.Prediction;
using App.Domain.Prediction;

namespace App.Application.Prediction;

public sealed class PredictionCalculationService : IPredictionCalculationService
{
    private readonly IErpBatchReader _erpBatchReader;
    private readonly PredictionContextBuilder _contextBuilder;
    private readonly PredictionOrchestrator _orchestrator;

    public PredictionCalculationService(
        IErpBatchReader erpBatchReader,
        PredictionContextBuilder contextBuilder,
        PredictionOrchestrator orchestrator)
    {
        _erpBatchReader = erpBatchReader ?? throw new ArgumentNullException(nameof(erpBatchReader));
        _contextBuilder = contextBuilder ?? throw new ArgumentNullException(nameof(contextBuilder));
        _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
    }

    public async Task<Result<PredictionResponse>> CalculateAsync(string orderReference, CancellationToken cancellationToken = default)
    {
        // 1. Read ERP Snapshot
        var snapshotResult = await _erpBatchReader.ReadAsync(orderReference, cancellationToken);
        if (!snapshotResult.IsSuccess)
        {
            return Result<PredictionResponse>.Failure(snapshotResult.Error!);
        }

        // 2. Build Context
        var (status, context) = _contextBuilder.Build(snapshotResult.Value!);
        if (status != DataSufficiency.Sufficient || context is null)
        {
            return Result<PredictionResponse>.Failure(new Error("Data.Insufficient", "ERP data is insufficient to run prediction.", ErrorType.Validation));
        }

        var aggregate = await _orchestrator.ExecuteAsync(context, cancellationToken);
        return Result<PredictionResponse>.Success(PredictionResponse.From(aggregate));
    }
}
