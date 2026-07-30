using MockErp.Api.Data;

namespace App.Integration.Tests.MockErp;

public sealed class MockErpDataStoreTests
{
    private static readonly string SeedPath = Path.Combine(
        AppContext.BaseDirectory,
        "Data",
        "mock-erp-seed.json");

    [Fact]
    public void SeedLoadsExpectedDeterministicData()
    {
        var store = new MockErpDataStore(SeedPath);

        Assert.Equal(2, store.GetOrders().Count);
        Assert.Equal("PROD-BIKE-01", store.GetOrder("ORD-1001")?.ProductId);
        Assert.Equal("Mock Commuter Bicycle", store.GetProduct("PROD-BIKE-01")?.Name);
        Assert.Collection(
            store.GetProductBom("PROD-BIKE-01"),
            line => Assert.Equal("COMP-FRAME-01", line.ComponentId),
            line => Assert.Equal("COMP-WHEEL-01", line.ComponentId));
    }

    [Fact]
    public void RepeatedReadsAreEquivalentAndUnknownIdsReturnNoData()
    {
        var store = new MockErpDataStore(SeedPath);

        Assert.Equal(store.GetOrders(), store.GetOrders());
        Assert.Equal(store.GetProductBom("PROD-BIKE-01"), store.GetProductBom("PROD-BIKE-01"));
        Assert.Null(store.GetOrder("ORD-UNKNOWN"));
        Assert.Null(store.GetProduct("PROD-UNKNOWN"));
        Assert.Empty(store.GetProductBom("PROD-UNKNOWN"));
    }

    [Fact]
    public void MissingSeedProducesClearFailure()
    {
        var missingPath = Path.Combine(AppContext.BaseDirectory, "missing-seed.json");

        var exception = Assert.Throws<InvalidOperationException>(
            () => new MockErpDataStore(missingPath));

        Assert.Contains("was not found", exception.Message);
    }
}
