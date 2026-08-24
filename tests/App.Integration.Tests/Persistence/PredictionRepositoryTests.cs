using App.Application.Contracts.Prediction;
using App.Application.Prediction;
using App.Domain.Entities;
using App.Persistence;
using App.Persistence.Prediction;
using Microsoft.EntityFrameworkCore;

namespace App.Integration.Tests.Persistence;

public sealed class PredictionRepositoryTests
{
    [Theory]
    [InlineData(FinalPredictionStatus.HybridCalculated)]
    [InlineData(FinalPredictionStatus.RuleBasedFallback)]
    [InlineData(FinalPredictionStatus.AiOnlyCandidate)]
    public async Task SaveAsync_TracksFinalAndBothProviderRowsInOneSave(FinalPredictionStatus status)
    {
        await using var context = CreateContext();
        var repository = new PredictionRepository(context);
        var payload = new AiFeaturePayload(1, "P", null, 1, 1, 0, 0, null, 1, 60,
            null, null, null, null, null, null, null);
        var rb = new PredictionProviderResult(PredictionProviderType.RuleBased, AiProviderStatus.Success, 100,
            Warnings: ["rb"], DurationMs: 5);
        var ai = new PredictionProviderResult(PredictionProviderType.Ai, AiProviderStatus.Success, 120,
            ModelVersion: "xgb-v0.1", FeatureSchemaVersion: "1", TrainingDatasetVersion: "synthetic-v1",
            FeaturePayload: payload, Warnings: ["ai"], DurationMs: 7);
        var final = new FinalPredictionResult(status,
            status == FinalPredictionStatus.HybridCalculated ? PredictionFallbackReason.None : PredictionFallbackReason.AiPredictionTimeout,
            status == FinalPredictionStatus.AiOnlyCandidate ? null : 108, "WeightedAverage", .6m, .4m, 20, 20);

        await repository.SaveAsync(new("O", rb, ai, final), new CancellationTokenSource().Token);

        Assert.Equal(1, context.SaveCalls);
        var aggregate = Assert.Single(context.ChangeTracker.Entries<PredictionResult>()).Entity;
        Assert.Equal(status.ToString(), aggregate.FinalStatus);
        var rows = context.ChangeTracker.Entries<PredictionProviderResultEntity>().Select(x => x.Entity).ToList();
        Assert.Equal(2, rows.Count);
        var rbRow = Assert.Single(rows, x => x.ProviderType == "RuleBased"); Assert.Null(rbRow.FeaturePayload);
        var aiRow = Assert.Single(rows, x => x.ProviderType == "Ai");
        Assert.Contains("productRef", aiRow.FeaturePayload!); Assert.Equal("xgb-v0.1", aiRow.ModelVersion);
        Assert.Equal("1", aiRow.FeatureSchemaVersion); Assert.Equal("synthetic-v1", aiRow.TrainingDatasetVersion);
        Assert.Equal(120, aiRow.WorkingLeadTimeMinutes); Assert.Equal(7, aiRow.DurationMs);
        Assert.Contains("ai", aiRow.Warnings!);
        Assert.Empty(context.ChangeTracker.Entries<IntegrationLog>());
    }

    private static TestContext CreateContext() => new(new DbContextOptionsBuilder<AppDbContext>()
        .UseNpgsql("Host=localhost;Database=change_tracker_only").Options);
    private sealed class TestContext(DbContextOptions<AppDbContext> options) : AppDbContext(options)
    {
        public int SaveCalls { get; private set; }
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        { SaveCalls++; return Task.FromResult(ChangeTracker.Entries().Count()); }
    }
}
