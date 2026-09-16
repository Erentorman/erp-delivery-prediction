using App.Application.Abstractions.Erp;
using App.Application.Common;
using App.Application.Contracts.Configuration;
using App.Application.Contracts.Erp;
using App.Application.Prediction;
using App.Domain.Prediction;
using Moq;

namespace App.Application.Tests.Prediction;

public sealed class PredictionCalculationServiceTests
{
    [Fact]
    public async Task CalculateAsync_BuildsContextOnceAndDelegatesToOrchestrator()
    {
        var reader = new Mock<IErpBatchReader>();
        reader.Setup(x => x.ReadAsync("ORD-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ErpBatchSnapshot>.Success(Snapshot()));
        var rb = new StubProvider(PredictionProviderType.RuleBased,
            new(PredictionProviderType.RuleBased, AiProviderStatus.Success, 60));
        var ai = new StubProvider(PredictionProviderType.Ai,
            new(PredictionProviderType.Ai, AiProviderStatus.Timeout));
        var repository = new StubRepository();
        var options = Options();
        var orchestrator = new PredictionOrchestrator([rb, ai], new FinalPredictionCombiner(options), repository, options);
        var service = new PredictionCalculationService(reader.Object, new PredictionContextBuilder(), orchestrator);

        var result = await service.CalculateAsync("ORD-1");

        Assert.True(result.IsSuccess);
        Assert.Equal("RuleBasedFallback", result.Value.FinalPrediction.Status);
        Assert.Equal(1, rb.Calls); Assert.Equal(1, ai.Calls); Assert.NotNull(repository.Saved);
    }

    [Fact]
    public async Task CalculateAsync_WhenErpDataIsInsufficient_ReturnsExistingFailure()
    {
        var reader = new Mock<IErpBatchReader>();
        reader.Setup(x => x.ReadAsync("ORD-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ErpBatchSnapshot>.Success(Snapshot() with { OrderItems = [] }));
        var options = Options();
        var service = new PredictionCalculationService(reader.Object, new PredictionContextBuilder(),
            new PredictionOrchestrator([
                new StubProvider(PredictionProviderType.RuleBased, new(PredictionProviderType.RuleBased, AiProviderStatus.Rejected)),
                new StubProvider(PredictionProviderType.Ai, new(PredictionProviderType.Ai, AiProviderStatus.Rejected))],
                new FinalPredictionCombiner(options), new StubRepository(), options));
        var result = await service.CalculateAsync("ORD-1");
        Assert.False(result.IsSuccess); Assert.Equal("Data.Insufficient", result.Error!.Code);
    }

    private static MvpAssumptionsOptions Options() => new()
    {
        WorkingCalendar = new()
        {
            StartTime = new TimeOnly(8, 0),
            EndTime = new TimeOnly(17, 0),
            BreakStartTime = new TimeOnly(12, 0),
            BreakEndTime = new TimeOnly(13, 0),
            BreakMinutes = 60,
            NetMinutesPerDay = 480,
            WorkingDays = [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday]
        },
        Procurement = new() { FallbackDurationMinutes = 960 },
        HybridPrediction = new()
    };

    private static ErpBatchSnapshot Snapshot() => new(DateTimeOffset.UtcNow,
        new("ORD-1", DateTimeOffset.UtcNow, null, null), [new("ORD-1", "I", "P", 1, "EA")],
        [new("P", "P", "EA")], [new("P", "C", 1, "EA", null)], [], [],
        [new("WO", null, "ORD-1", "P", new("R", [new("OP", 1, "WC", 60, [])]))]);

    private sealed class StubProvider(PredictionProviderType type, PredictionProviderResult result) : IPredictionProvider
    {
        public int Calls { get; private set; }
        public PredictionProviderType ProviderType => type;
        public Task<PredictionProviderResult> PredictAsync(PredictionContext context, CancellationToken cancellationToken = default)
        { Calls++; return Task.FromResult(result); }
    }
    private sealed class StubRepository : IPredictionRepository
    {
        public PredictionAggregateResult? Saved { get; private set; }
        public Task SaveAsync(PredictionAggregateResult result, CancellationToken cancellationToken = default)
        { Saved = result; return Task.CompletedTask; }
        public Task SaveAsync(PredictionPersistenceRequest request, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IReadOnlyList<PredictionHistoryListItem>> GetHistoryAsync(string? orderReference, int page, int pageSize, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<PredictionHistoryListItem>>([]);
        public Task<PredictionHistoryDetail?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
            => Task.FromResult<PredictionHistoryDetail?>(null);
    }
}
