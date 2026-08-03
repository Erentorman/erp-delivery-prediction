using System.Text.Json;
using MockErp.Api.Data;

namespace App.Integration.Tests.MockErp;

public sealed class ErpSeedConverterPreviewSmokeTests
{
    private static readonly string FixtureDirectory = Path.Combine(
        AppContext.BaseDirectory,
        "Fixtures",
        "ErpSeedConverterPreview");

    private static readonly string SeedPath =
        Path.Combine(FixtureDirectory, "mock-erp-seed.json");

    private static readonly string ValidationReportPath =
        Path.Combine(FixtureDirectory, "validation-report.json");

    private static readonly string GroundTruthPath =
        Path.Combine(FixtureDirectory, "prediction-ground-truth.json");

    private static readonly string MaterialDictionaryPath =
        Path.Combine(FixtureDirectory, "material-dictionary-provisional.json");

    [Fact]
    public void GeneratedPreviewSeedLoadsThroughProductionMockErpDataStore()
    {
        Assert.True(File.Exists(SeedPath), $"Preview seed was not copied to '{SeedPath}'.");
        Assert.True(File.Exists(ValidationReportPath));
        Assert.True(File.Exists(GroundTruthPath));
        Assert.True(File.Exists(MaterialDictionaryPath));

        // Gerçek production deserialize yolu.
        var store = new MockErpDataStore(SeedPath);

        using var seedDocument = JsonDocument.Parse(File.ReadAllText(SeedPath));
        var root = seedDocument.RootElement;

        var ordersElement = GetRequiredArray(root, "orders");
        var productsElement = GetRequiredArray(root, "products");
        var bomsElement = GetRequiredArray(root, "boms");
        var stockLevelsElement = GetRequiredArray(root, "stockLevels");
        var openPurchaseOrdersElement = GetRequiredArray(root, "openPurchaseOrders");
        var workOrdersElement = GetRequiredArray(root, "workOrders");
        var shippingDurationsElement = GetRequiredArray(root, "shippingDurations");

        Assert.True(root.TryGetProperty("capacityCalendar", out var capacityCalendar));
        Assert.Equal(JsonValueKind.Object, capacityCalendar.ValueKind);

        var workCentersElement = GetRequiredArray(capacityCalendar, "workCenters");
        var shiftsElement = GetRequiredArray(capacityCalendar, "shifts");
        var holidaysElement = GetRequiredArray(capacityCalendar, "holidays");
        var plannedDowntimesElement = GetRequiredArray(
            capacityCalendar,
            "plannedDowntimes");

        Assert.Equal(5, ordersElement.GetArrayLength());
        Assert.Equal(4, productsElement.GetArrayLength());
        Assert.Equal(4, bomsElement.GetArrayLength());
        Assert.Equal(4, stockLevelsElement.GetArrayLength());

        var orders = store.GetOrders();
        Assert.Equal(5, orders.Count);

        var productIds = productsElement
            .EnumerateArray()
            .Select(product => product.GetProperty("id").GetString())
            .Where(productId => productId is not null)
            .Select(productId => productId!)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Equal(4, productIds.Count);

        foreach (var orderElement in ordersElement.EnumerateArray())
        {
            var orderId = orderElement.GetProperty("id").GetString();
            var productId = orderElement.GetProperty("productId").GetString();
            var requestedDeliveryDate =
                orderElement.GetProperty("requestedDeliveryDate").GetString();

            Assert.NotNull(orderId);
            Assert.NotNull(productId);
            Assert.NotNull(requestedDeliveryDate);
            Assert.Contains(productId!, productIds);
            Assert.NotNull(store.GetProduct(productId!));

            var order = store.GetOrder(orderId!);
            Assert.NotNull(order);
            Assert.Equal(
                DateOnly.Parse(requestedDeliveryDate!),
                order.RequestedDeliveryDate);
        }

        var totalBomLines = 0;

        foreach (var bomElement in bomsElement.EnumerateArray())
        {
            var productId = bomElement.GetProperty("productId").GetString();
            Assert.NotNull(productId);
            Assert.Contains(productId!, productIds);
            Assert.NotNull(store.GetProduct(productId!));

            var lines = GetRequiredArray(bomElement, "lines");
            totalBomLines += lines.GetArrayLength();

            Assert.Equal(
                lines.GetArrayLength(),
                store.GetProductBom(productId!).Count);
        }

        Assert.Equal(51, totalBomLines);

        var stockProductReferences = stockLevelsElement
            .EnumerateArray()
            .Select(stock => stock.GetProperty("productReference").GetString())
            .Where(productReference => productReference is not null)
            .Select(productReference => productReference!)
            .ToArray();

        Assert.All(
            stockProductReferences,
            productReference => Assert.NotNull(store.GetProduct(productReference)));

        Assert.Equal(
            4,
            store.GetStockLevels(stockProductReferences).Count);

        var rangeStart = DateTimeOffset.Parse("2026-07-01T00:00:00+03:00");
        var rangeEnd = DateTimeOffset.Parse("2026-07-31T23:59:59+03:00");

        var capacityResult = store.GetCapacityAndCalendar(
            Array.Empty<string>(),
            rangeStart,
            rangeEnd);

        Assert.NotNull(capacityResult);
        Assert.NotNull(capacityResult.WorkCenters);
        Assert.NotNull(capacityResult.Shifts);
        Assert.NotNull(capacityResult.Holidays);
        Assert.NotNull(capacityResult.PlannedDowntimes);

        Assert.Empty(workCentersElement.EnumerateArray());
        Assert.Empty(shiftsElement.EnumerateArray());
        Assert.Empty(holidaysElement.EnumerateArray());
        Assert.Empty(plannedDowntimesElement.EnumerateArray());

        Assert.Empty(openPurchaseOrdersElement.EnumerateArray());
        Assert.Empty(workOrdersElement.EnumerateArray());
        Assert.Empty(shippingDurationsElement.EnumerateArray());

        Assert.Empty(store.GetOpenPurchaseOrders(productIds));
        Assert.Empty(store.GetWorkOrders("SO00001", productIds));
        Assert.Null(store.GetShippingDuration("UNKNOWN", "UNKNOWN", "UNKNOWN"));

        // Camel-case JSON property naming kontrolleri.
        Assert.False(root.TryGetProperty("Orders", out _));
        Assert.False(root.TryGetProperty("Products", out _));
        Assert.True(
            ordersElement[0].TryGetProperty("requestedDeliveryDate", out _));
        Assert.False(
            ordersElement[0].TryGetProperty("RequestedDeliveryDate", out _));

        using var validationDocument =
            JsonDocument.Parse(File.ReadAllText(ValidationReportPath));

        var validationRoot = validationDocument.RootElement;
        Assert.True(validationRoot.GetProperty("isValid").GetBoolean());

        var recordCounts = validationRoot.GetProperty("recordCounts");

        Assert.Equal(
            orders.Count,
            recordCounts.GetProperty("orders").GetInt32());

        Assert.Equal(
            productsElement.GetArrayLength(),
            recordCounts.GetProperty("products").GetInt32());

        Assert.Equal(
            bomsElement.GetArrayLength(),
            recordCounts.GetProperty("boms").GetInt32());

        Assert.Equal(
            store.GetStockLevels(stockProductReferences).Count,
            recordCounts.GetProperty("stockLevels").GetInt32());

        using var groundTruthDocument =
            JsonDocument.Parse(File.ReadAllText(GroundTruthPath));

        using var materialDictionaryDocument =
            JsonDocument.Parse(File.ReadAllText(MaterialDictionaryPath));

        Assert.Equal(
            groundTruthDocument.RootElement.GetArrayLength(),
            recordCounts.GetProperty("groundTruth").GetInt32());

        Assert.Equal(
            materialDictionaryDocument.RootElement.GetArrayLength(),
            recordCounts.GetProperty("uniqueMaterials").GetInt32());
    }

    [Theory]
    [InlineData("prediction-ground-truth.json")]
    [InlineData("material-dictionary-provisional.json")]
    public void AuxiliaryPreviewArtifactsAreNotValidMockErpRuntimeSeeds(
        string fileName)
    {
        var path = Path.Combine(FixtureDirectory, fileName);

        Assert.True(File.Exists(path));

        var exception = Assert.Throws<InvalidOperationException>(
            () => new MockErpDataStore(path));

        Assert.Contains("contains invalid JSON", exception.Message);
    }

    private static JsonElement GetRequiredArray(
        JsonElement parent,
        string propertyName)
    {
        Assert.True(
            parent.TryGetProperty(propertyName, out var property),
            $"Required JSON property '{propertyName}' was not found.");

        Assert.Equal(JsonValueKind.Array, property.ValueKind);
        return property;
    }
}