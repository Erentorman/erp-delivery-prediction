using System.Text.Json;
using MockErp.Api.Data;
using MockErp.Api.Models;

namespace App.Integration.Tests.MockErp;

public sealed class ShippingRouteContractTests
{
    private static readonly string FixturePath = Path.Combine(
        AppContext.BaseDirectory,
        "Fixtures",
        "MockErp",
        "ShippingRouteContract",
        "mock-erp-seed.json");

    [Fact]
    public void FixtureLoadsAndExistingRouteReturnsExactPositiveDuration()
    {
        var store = new MockErpDataStore(FixturePath);

        var route = store.GetShippingDuration("WH-IST-01", "CUSTOMER-ANK-01", "STANDARD");

        Assert.NotNull(route);
        Assert.Equal(1_440L, route.ShippingDurationMinutes);
        Assert.Null(store.GetShippingDuration("WH-IST-01", "UNKNOWN", "STANDARD"));
    }

    [Fact]
    public void ShippingModelAndFixtureUseOnlyExpectedProperties()
    {
        var expected = new[]
        {
            "originReference",
            "destinationReference",
            "shippingProfileReference",
            "shippingDurationMinutes"
        };
        var modelProperties = typeof(MockErpShippingRoute).GetProperties()
            .Select(property => JsonNamingPolicy.CamelCase.ConvertName(property.Name));
        using var document = JsonDocument.Parse(File.ReadAllText(FixturePath));
        var fixtureProperties = document.RootElement.GetProperty("shippingDurations")[0]
            .EnumerateObject()
            .Select(property => property.Name);

        Assert.Equal(expected, modelProperties);
        Assert.Equal(expected, fixtureProperties);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void NonPositiveFixtureDurationFailsFast(long duration)
    {
        var seedPath = WriteTempSeed(duration);

        var exception = Assert.Throws<InvalidOperationException>(() => new MockErpDataStore(seedPath));

        Assert.Contains("ShippingDurationMinutes", exception.Message);
        Assert.Contains(duration.ToString(), exception.Message);
    }

    [Fact]
    public void DuplicateRouteIdentityFailsFast()
    {
        var seedPath = WriteTempSeed(60, duplicate: true);

        Assert.Contains(
            "duplicate shipping route",
            Assert.Throws<InvalidOperationException>(() => new MockErpDataStore(seedPath)).Message);
    }

    private static string WriteTempSeed(long duration, bool duplicate = false)
    {
        var route = $$"""
            {
              "originReference": "WH-IST-01",
              "destinationReference": "CUSTOMER-ANK-01",
              "shippingProfileReference": "STANDARD",
              "shippingDurationMinutes": {{duration}}
            }
            """;
        var routes = duplicate ? $"{route},{route}" : route;
        var json = $$"""
            {
              "orders": [],
              "products": [],
              "boms": [],
              "stockLevels": [],
              "openPurchaseOrders": [],
              "workOrders": [],
              "capacityCalendar": {
                "workCenters": [],
                "shifts": [],
                "holidays": [],
                "plannedDowntimes": []
              },
              "shippingDurations": [{{routes}}]
            }
            """;
        var path = Path.Combine(Path.GetTempPath(), $"mock-erp-seed-shipping-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, json);
        return path;
    }
}
