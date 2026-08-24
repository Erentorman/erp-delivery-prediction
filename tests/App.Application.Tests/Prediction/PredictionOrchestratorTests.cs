using App.Application.Contracts.Configuration;
using App.Application.Prediction;
using App.Domain.Prediction;

namespace App.Application.Tests.Prediction;

public sealed class PredictionOrchestratorTests
{
    [Fact]
    public async Task Execute_StartsAiBeforeRuleBased_ThenAwaitsAndPersists()
    {
        var ai = new DelayedAiProvider();
        var rule = new AssertingRuleProvider(() => ai.Started);
        var repository = new CapturingRepository();
        var options = new MvpAssumptionsOptions { WorkingCalendar = new() { MinutesPerDay = 480 }, HybridPrediction = new() };
        var orchestrator = new PredictionOrchestrator([rule, ai], new FinalPredictionCombiner(options), repository, options);

        var task = orchestrator.ExecuteAsync(Context());
        Assert.True(ai.Started); Assert.True(rule.Executed); Assert.False(task.IsCompleted);
        ai.Complete();
        var result = await task;

        Assert.Same(result, repository.Saved);
        Assert.Equal(FinalPredictionStatus.HybridCalculated, result.FinalPrediction.Status);
    }

    private static PredictionContext Context() => new(new("O", "P", 1, DateTimeOffset.UtcNow),
        new([], [], [], []), new([]), new(), new(), new());
    private sealed class DelayedAiProvider : IPredictionProvider
    {
        private readonly TaskCompletionSource<PredictionProviderResult> _source = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public bool Started { get; private set; }
        public PredictionProviderType ProviderType => PredictionProviderType.Ai;
        public Task<PredictionProviderResult> PredictAsync(PredictionContext context, CancellationToken cancellationToken = default)
        { Started = true; cancellationToken.Register(() => _source.TrySetCanceled(cancellationToken)); return _source.Task; }
        public void Complete() => _source.SetResult(new(PredictionProviderType.Ai, AiProviderStatus.Success, 100));
    }
    private sealed class AssertingRuleProvider(Func<bool> aiStarted) : IPredictionProvider
    {
        public bool Executed { get; private set; }
        public PredictionProviderType ProviderType => PredictionProviderType.RuleBased;
        public Task<PredictionProviderResult> PredictAsync(PredictionContext context, CancellationToken cancellationToken = default)
        { Assert.True(aiStarted()); Executed = true; return Task.FromResult(new PredictionProviderResult(ProviderType, AiProviderStatus.Success, 100)); }
    }
    private sealed class CapturingRepository : IPredictionRepository
    {
        public PredictionAggregateResult? Saved { get; private set; }
        public Task SaveAsync(PredictionAggregateResult result, CancellationToken cancellationToken = default) { Saved = result; return Task.CompletedTask; }
    }
}
