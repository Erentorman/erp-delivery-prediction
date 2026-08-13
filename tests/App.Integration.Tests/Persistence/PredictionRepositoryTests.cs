using System.Text.Json;
using App.Application.Prediction;
using App.Persistence;
using App.Persistence.Prediction;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace App.Integration.Tests.Persistence;

public class PredictionRepositoryTests
{
    private static AppDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new AppDbContext(options);
    }

    private static RuleBasedPredictionResult CreateResult(string orderReference, DateTimeOffset start)
    {
        var end = start.AddMinutes(60);
        var delivery = end.AddMinutes(1440);
        return new RuleBasedPredictionResult(
            orderReference,
            start,
            end,
            delivery,
            new[] { "OP-1" },
            new[] { "No Open PO found, using fallback lead time" },
            new[] { new MaterialShortage("MAT-1", 5m) },
            new[] { new TimelineItem("OP-1", start, end, true) });
    }

    [Fact]
    public async Task SaveAsync_RealOrderPrediction_CreatesResultAndRuleBasedProviderRow()
    {
        await using var db = CreateContext(nameof(SaveAsync_RealOrderPrediction_CreatesResultAndRuleBasedProviderRow));
        var repo = new PredictionRepository(db);
        var start = new DateTimeOffset(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);

        await repo.SaveAsync(new PredictionPersistenceRequest(
            "SO00001", false, null, start.AddDays(-1), CreateResult("SO00001", start)));

        var saved = Assert.Single(db.PredictionResults);
        Assert.Equal("SO00001", saved.ErpOrderRef);
        Assert.False(saved.IsSimulation);
        Assert.Null(saved.SimulationInputSummary);
        Assert.Equal("CalculatedWithAssumptions", saved.Status);

        var providerRow = Assert.Single(db.PredictionProviderResults);
        Assert.Equal(saved.Id, providerRow.PredictionResultId);
        Assert.Equal("RuleBased", providerRow.ProviderType);
        Assert.Equal("Success", providerRow.ProviderStatus);
    }

    [Fact]
    public async Task SaveAsync_CalledTwiceForSameOrder_CreatesTwoSeparateHistoryRows()
    {
        await using var db = CreateContext(nameof(SaveAsync_CalledTwiceForSameOrder_CreatesTwoSeparateHistoryRows));
        var repo = new PredictionRepository(db);
        var start = new DateTimeOffset(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);

        await repo.SaveAsync(new PredictionPersistenceRequest("SO00001", false, null, null, CreateResult("SO00001", start)));
        await repo.SaveAsync(new PredictionPersistenceRequest("SO00001", false, null, null, CreateResult("SO00001", start.AddHours(4))));

        var rows = db.PredictionResults.Where(p => p.ErpOrderRef == "SO00001").ToList();
        Assert.Equal(2, rows.Count);
        Assert.NotEqual(rows[0].Id, rows[1].Id);
    }

    [Fact]
    public async Task SaveAsync_WhatIf_PersistsWithIsSimulationTrueAndNoErpOrderRef()
    {
        await using var db = CreateContext(nameof(SaveAsync_WhatIf_PersistsWithIsSimulationTrueAndNoErpOrderRef));
        var repo = new PredictionRepository(db);
        var start = new DateTimeOffset(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);

        await repo.SaveAsync(new PredictionPersistenceRequest(
            null,
            true,
            new WhatIfSimulationInputSummary("P002", 5m, "ankara"),
            null,
            CreateResult("WHATIF-P002", start)));

        var saved = Assert.Single(db.PredictionResults);
        Assert.Null(saved.ErpOrderRef);
        Assert.True(saved.IsSimulation);
        Assert.NotNull(saved.SimulationInputSummary);

        using var doc = JsonDocument.Parse(saved.SimulationInputSummary!);
        Assert.Equal("P002", doc.RootElement.GetProperty("productReference").GetString());
        Assert.Equal(5m, doc.RootElement.GetProperty("quantity").GetDecimal());
        Assert.Equal("ankara", doc.RootElement.GetProperty("locationReference").GetString());
    }

    [Fact]
    public async Task GetHistoryAsync_ReturnsRowsOrderedByCalculatedAtDescending()
    {
        await using var db = CreateContext(nameof(GetHistoryAsync_ReturnsRowsOrderedByCalculatedAtDescending));
        var repo = new PredictionRepository(db);
        var start = new DateTimeOffset(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);

        await repo.SaveAsync(new PredictionPersistenceRequest("SO00001", false, null, null, CreateResult("SO00001", start)));
        await repo.SaveAsync(new PredictionPersistenceRequest("SO00001", false, null, null, CreateResult("SO00001", start.AddHours(4))));
        await repo.SaveAsync(new PredictionPersistenceRequest("SO00002", false, null, null, CreateResult("SO00002", start.AddHours(2))));

        var history = await repo.GetHistoryAsync(null, 1, 20);

        Assert.Equal(3, history.Count);
        Assert.True(history[0].CalculatedAt >= history[1].CalculatedAt);
        Assert.True(history[1].CalculatedAt >= history[2].CalculatedAt);
    }

    [Fact]
    public async Task GetHistoryAsync_FilterByOrderReference_ReturnsOnlyMatchingRealOrderRows()
    {
        await using var db = CreateContext(nameof(GetHistoryAsync_FilterByOrderReference_ReturnsOnlyMatchingRealOrderRows));
        var repo = new PredictionRepository(db);
        var start = new DateTimeOffset(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);

        await repo.SaveAsync(new PredictionPersistenceRequest("SO00001", false, null, null, CreateResult("SO00001", start)));
        await repo.SaveAsync(new PredictionPersistenceRequest("SO00002", false, null, null, CreateResult("SO00002", start)));
        await repo.SaveAsync(new PredictionPersistenceRequest(
            null, true, new WhatIfSimulationInputSummary("P002", 5m, "ankara"), null, CreateResult("WHATIF-P002", start)));

        var history = await repo.GetHistoryAsync("SO00001", 1, 20);

        var item = Assert.Single(history);
        Assert.Equal("SO00001", item.ErpOrderRef);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsFullDetailIncludingProviderResults()
    {
        await using var db = CreateContext(nameof(GetByIdAsync_ReturnsFullDetailIncludingProviderResults));
        var repo = new PredictionRepository(db);
        var start = new DateTimeOffset(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);

        await repo.SaveAsync(new PredictionPersistenceRequest("SO00001", false, null, start.AddDays(-2), CreateResult("SO00001", start)));
        var savedId = db.PredictionResults.Single().Id;

        var detail = await repo.GetByIdAsync(savedId);

        Assert.NotNull(detail);
        Assert.Equal("SO00001", detail!.ErpOrderRef);
        var provider = Assert.Single(detail.ProviderResults);
        Assert.Equal("RuleBased", provider.ProviderType);
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotFound_ReturnsNull()
    {
        await using var db = CreateContext(nameof(GetByIdAsync_WhenNotFound_ReturnsNull));
        var repo = new PredictionRepository(db);

        var detail = await repo.GetByIdAsync(999);

        Assert.Null(detail);
    }

    [Fact]
    public async Task TrainingCandidateFilter_ExcludesWhatIfRows()
    {
        await using var db = CreateContext(nameof(TrainingCandidateFilter_ExcludesWhatIfRows));
        var repo = new PredictionRepository(db);
        var start = new DateTimeOffset(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);

        await repo.SaveAsync(new PredictionPersistenceRequest("SO00001", false, null, null, CreateResult("SO00001", start)));
        await repo.SaveAsync(new PredictionPersistenceRequest(
            null, true, new WhatIfSimulationInputSummary("P002", 5m, "ankara"), null, CreateResult("WHATIF-P002", start)));

        // Mirrors the SAD §18.4 training-candidate filter:
        // is_simulation = false AND erp_order_ref IS NOT NULL (AND actual_delivery_date IS NOT NULL, once populated).
        var candidates = db.PredictionResults
            .Where(p => !p.IsSimulation && p.ErpOrderRef != null)
            .ToList();

        var candidate = Assert.Single(candidates);
        Assert.Equal("SO00001", candidate.ErpOrderRef);
        Assert.DoesNotContain(candidates, c => c.IsSimulation);
    }
}
