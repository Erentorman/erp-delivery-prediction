using System.Diagnostics;
using App.Domain.Prediction;

namespace App.Application.Prediction;

public sealed class RuleBasedPredictionProvider : IPredictionProvider
{
    private readonly RuleBasedPredictionEngine _engine;
    private readonly ICriticalPathCalculator _criticalPathCalculator;
    private readonly PredictionResultMapper _mapper;

    public RuleBasedPredictionProvider(RuleBasedPredictionEngine engine, ICriticalPathCalculator criticalPathCalculator, PredictionResultMapper mapper)
    {
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        _criticalPathCalculator = criticalPathCalculator ?? throw new ArgumentNullException(nameof(criticalPathCalculator));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    public PredictionProviderType ProviderType => PredictionProviderType.RuleBased;

    public Task<PredictionProviderResult> PredictAsync(PredictionContext context, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var stopwatch = Stopwatch.StartNew();
        var engineResult = _engine.Run(context);
        if (!engineResult.Success)
            return Task.FromResult(Failure("RuleBasedEngineError", stopwatch));

        var cpm = _criticalPathCalculator.Calculate(engineResult.Context);
        if (cpm.Status != CriticalPathStatus.Success || cpm.Result is null)
        {
            var code = cpm.Status == CriticalPathStatus.CycleDetected
                ? "OperationGraphCycleDetected"
                : "InvalidErpData";
            return Task.FromResult(Failure(code, stopwatch));
        }

        var mapped = _mapper.Map(context.OrderInput.OrderReference, engineResult, cpm);
        if (!mapped.IsSuccess)
            return Task.FromResult(Failure("InvalidErpData", stopwatch));

        stopwatch.Stop();
        return Task.FromResult(new PredictionProviderResult(
            ProviderType, AiProviderStatus.Success, cpm.Result.TotalWorkingMinutes,
            mapped.Value, DurationMs: stopwatch.ElapsedMilliseconds));
    }

    private PredictionProviderResult Failure(string code, Stopwatch stopwatch)
    {
        stopwatch.Stop();
        return new PredictionProviderResult(ProviderType, AiProviderStatus.Rejected,
            DurationMs: stopwatch.ElapsedMilliseconds, FailureCode: code);
    }
}
