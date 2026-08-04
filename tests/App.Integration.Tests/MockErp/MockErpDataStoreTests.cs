using System.Collections;
using System.Text.Json;
using MockErp.Api.Data;
using MockErp.Api.Models;

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

    [Fact]
    public void SeedContainsAllRemainingResourceCategories()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(SeedPath));
        var root = document.RootElement;

        Assert.True(root.GetProperty("stockLevels").GetArrayLength() > 1);
        Assert.True(root.GetProperty("openPurchaseOrders").GetArrayLength() > 1);
        Assert.True(root.GetProperty("workOrders").GetArrayLength() > 1);
        Assert.True(root.GetProperty("capacityCalendar").GetProperty("workCenters").GetArrayLength() > 1);
        Assert.True(root.GetProperty("shippingDurations").GetArrayLength() > 1);
    }

    [Fact]
    public void StockLevelsFilterAndPreserveExactDecimals()
    {
        var store = new MockErpDataStore(SeedPath);

        var result = store.GetStockLevels(["PROD-BIKE-01"]);

        var stock = Assert.Single(result);
        Assert.Equal(20.50m, stock.OnHandQuantity);
        Assert.Equal(8.25m, stock.ReservedQuantity);
        Assert.Equal(12.25m, stock.AvailableQuantity);
        Assert.Empty(store.GetStockLevels(["UNKNOWN"]));
    }

    [Fact]
    public void OpenPurchaseOrdersFilterAndPreserveDateOffset()
    {
        var store = new MockErpDataStore(SeedPath);

        var purchaseOrder = Assert.Single(store.GetOpenPurchaseOrders(["PROD-DESK-01"]));

        Assert.Equal("PO-2002", purchaseOrder.PurchaseOrderReference);
        Assert.Equal(6.25m, purchaseOrder.OpenQuantity);
        Assert.Equal(TimeSpan.FromHours(3), purchaseOrder.ExpectedAvailabilityDateTime.Offset);
        Assert.Equal(5760, purchaseOrder.SupplierLeadTimeMinutes);
    }

    [Fact]
    public void WorkOrdersFilterAndPreserveNestedOperations()
    {
        var store = new MockErpDataStore(SeedPath);

        var workOrder = Assert.Single(
            store.GetWorkOrders("ORD-1001", ["PROD-BIKE-01", "PROD-DESK-01"]));

        Assert.Equal("WO-3001", workOrder.WorkOrderReference);
        Assert.Collection(
            workOrder.Operations,
            operation =>
            {
                Assert.Equal(180, operation.StandardDurationMinutes);
                Assert.Empty(operation.PredecessorOperationReferences);
            },
            operation =>
            {
                Assert.Equal(45, operation.RemainingDurationMinutes);
                Assert.Equal(["WO-3001-OP10"], operation.PredecessorOperationReferences);
            });
        Assert.Empty(store.GetWorkOrders("ORD-1001", ["PROD-DESK-01"]));
    }

    [Fact]
    public void CapacityCalendarFiltersReferencesAndRangeWhilePreservingOffsets()
    {
        var store = new MockErpDataStore(SeedPath);
        var rangeStart = DateTimeOffset.Parse("2026-08-04T00:00:00+03:00");
        var rangeEnd = DateTimeOffset.Parse("2026-08-05T23:59:59+03:00");

        var result = store.GetCapacityAndCalendar(
            ["WC-ASSEMBLY-01"],
            rangeStart,
            rangeEnd);

        Assert.Equal(rangeStart, result.RangeStart);
        Assert.Equal(rangeEnd, result.RangeEnd);
        Assert.Single(result.WorkCenters);
        Assert.Equal("WC-ASSEMBLY-01", result.WorkCenters[0].WorkCenterRef);
        Assert.Equal(2, result.WorkCenters[0].MachineCount);
        Assert.Single(result.Shifts);
        Assert.Equal(TimeSpan.FromHours(3), result.Shifts[0].Start.Offset);
        Assert.Single(result.Holidays);
        Assert.Single(result.PlannedDowntimes);
        Assert.Equal(60, result.PlannedDowntimes[0].PlannedDowntimeMinutes);
    }

    [Fact]
    public void ShippingDurationReturnsMatchAndNullForUnknownRoute()
    {
        var store = new MockErpDataStore(SeedPath);

        var duration = store.GetShippingDuration(
            "WH-IST-01",
            "CUSTOMER-ANK-01",
            "STANDARD");

        Assert.NotNull(duration);
        Assert.Equal("ROUTE-IST-ANK", duration.RoutingReference);
        Assert.Equal(720, duration.ShippingDurationMinutes);
        Assert.Null(store.GetShippingDuration("UNKNOWN", "UNKNOWN", "UNKNOWN"));
    }

    [Fact]
    public void RepeatedReadsAreDeterministicAndReturnedCollectionsAreReadOnly()
    {
        var store = new MockErpDataStore(SeedPath);
        var first = store.GetStockLevels(["PROD-BIKE-01"]);
        var second = store.GetStockLevels(["PROD-BIKE-01"]);
        var workOrder = Assert.Single(store.GetWorkOrders("ORD-1001", ["PROD-BIKE-01"]));

        Assert.Equal(first, second);
        Assert.Throws<NotSupportedException>(
            () => ((IList)first).Add(new MockErpStockLevel("X", null, 0, 0, 0)));
        Assert.Throws<NotSupportedException>(
            () => ((IList)workOrder.Operations).Clear());
        Assert.Throws<NotSupportedException>(
            () => ((IList)workOrder.Operations[1].PredecessorOperationReferences).Clear());
        Assert.Single(store.GetStockLevels(["PROD-BIKE-01"]));
    }

    [Fact]
    public void WorkCenterMachineCountMustBeAtLeastOne()
    {
        var invalidSeedPath = Path.Combine(AppContext.BaseDirectory, "invalid-wc-machinecount.json");
        File.WriteAllText(invalidSeedPath, """
            {
                "orders": [], "products": [], "boms": [], "stockLevels": [], "openPurchaseOrders": [], "workOrders": [], "shippingDurations": [],
                "capacityCalendar": {
                    "workCenters": [ { "workCenterRef": "WC-1", "name": "A", "machineCount": 0 } ],
                    "shifts": [], "holidays": [], "plannedDowntimes": []
                }
            }
            """);

        var exception = Assert.Throws<InvalidOperationException>(() => new MockErpDataStore(invalidSeedPath));
        Assert.Contains("machine count of 1 or more", exception.Message);
        
        File.Delete(invalidSeedPath);
    }

    [Fact]
    public void WorkCenterReferencesMustBeUnique()
    {
        var invalidSeedPath = Path.Combine(AppContext.BaseDirectory, "invalid-wc-duplicate.json");
        File.WriteAllText(invalidSeedPath, """
            {
                "orders": [], "products": [], "boms": [], "stockLevels": [], "openPurchaseOrders": [], "workOrders": [], "shippingDurations": [],
                "capacityCalendar": {
                    "workCenters": [ 
                        { "workCenterRef": "WC-1", "name": "A", "machineCount": 1 },
                        { "workCenterRef": "WC-1", "name": "B", "machineCount": 2 } 
                    ],
                    "shifts": [], "holidays": [], "plannedDowntimes": []
                }
            }
            """);

        var exception = Assert.Throws<InvalidOperationException>(() => new MockErpDataStore(invalidSeedPath));
        Assert.Contains("duplicate work center", exception.Message);
        
        File.Delete(invalidSeedPath);
    }
}
