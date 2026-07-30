using System.Collections.ObjectModel;
using System.Text.Json;
using MockErp.Api.Models;

namespace MockErp.Api.Data;

public sealed class MockErpDataStore
{
    private const string SeedRelativePath = "Data/mock-erp-seed.json";
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly IReadOnlyList<MockErpOrder> _orders;
    private readonly IReadOnlyDictionary<string, MockErpOrder> _ordersById;
    private readonly IReadOnlyList<MockErpProduct> _products;
    private readonly IReadOnlyDictionary<string, MockErpProduct> _productsById;
    private readonly IReadOnlyDictionary<string, IReadOnlyList<MockErpBomLine>> _bomsByProductId;

    public MockErpDataStore(IHostEnvironment environment)
        : this(Path.Combine(environment.ContentRootPath, SeedRelativePath))
    {
    }

    public MockErpDataStore(string seedPath)
    {
        if (!File.Exists(seedPath))
        {
            throw new InvalidOperationException($"Mock ERP seed file was not found at '{seedPath}'.");
        }

        SeedDocument seed;
        try
        {
            using var stream = File.OpenRead(seedPath);
            seed = JsonSerializer.Deserialize<SeedDocument>(stream, SerializerOptions)
                ?? throw new InvalidOperationException("Mock ERP seed JSON deserialized to null.");
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException(
                $"Mock ERP seed file '{seedPath}' contains invalid JSON.",
                exception);
        }

        if (seed.Orders is null || seed.Products is null || seed.Boms is null)
        {
            throw new InvalidOperationException(
                "Mock ERP seed must contain orders, products, and boms collections.");
        }

        EnsureUnique(seed.Orders.Select(order => order.Id), "order");
        EnsureUnique(seed.Products.Select(product => product.Id), "product");
        EnsureUnique(seed.Boms.Select(bom => bom.ProductId), "BOM product");

        var orders = seed.Orders
            .Select(order => new MockErpOrder(
                order.Id, order.ProductId, order.Quantity, order.RequestedDeliveryDate))
            .ToArray();
        var products = seed.Products
            .Select(product => new MockErpProduct(product.Id, product.Name, product.Unit))
            .ToArray();

        _orders = Array.AsReadOnly(orders);
        _products = Array.AsReadOnly(products);
        _ordersById = new ReadOnlyDictionary<string, MockErpOrder>(
            orders.ToDictionary(order => order.Id, StringComparer.Ordinal));
        _productsById = new ReadOnlyDictionary<string, MockErpProduct>(
            products.ToDictionary(product => product.Id, StringComparer.Ordinal));
        _bomsByProductId = new ReadOnlyDictionary<string, IReadOnlyList<MockErpBomLine>>(
            seed.Boms.ToDictionary(
                bom => bom.ProductId,
                bom => (IReadOnlyList<MockErpBomLine>)Array.AsReadOnly(
                    (bom.Lines ?? throw new InvalidOperationException(
                        $"BOM for product '{bom.ProductId}' is missing its lines collection."))
                    .Select(line => new MockErpBomLine(
                        line.ComponentId, line.Description, line.Quantity, line.Unit))
                    .ToArray()),
                StringComparer.Ordinal));
    }

    public IReadOnlyList<MockErpOrder> GetOrders() => _orders;

    public MockErpOrder? GetOrder(string id) =>
        _ordersById.GetValueOrDefault(id);

    public MockErpProduct? GetProduct(string id) =>
        _productsById.GetValueOrDefault(id);

    public IReadOnlyList<MockErpBomLine> GetProductBom(string productId) =>
        _bomsByProductId.GetValueOrDefault(productId) ?? Array.Empty<MockErpBomLine>();

    private static void EnsureUnique(IEnumerable<string> identifiers, string identifierType)
    {
        var duplicate = identifiers
            .GroupBy(identifier => identifier, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicate is not null)
        {
            throw new InvalidOperationException(
                $"Mock ERP seed contains duplicate {identifierType} identifier '{duplicate.Key}'.");
        }
    }

    private sealed record SeedDocument(
        List<SeedOrder>? Orders,
        List<SeedProduct>? Products,
        List<SeedBom>? Boms);

    private sealed record SeedOrder(
        string Id,
        string ProductId,
        int Quantity,
        DateOnly RequestedDeliveryDate);

    private sealed record SeedProduct(
        string Id,
        string Name,
        string Unit);

    private sealed record SeedBom(
        string ProductId,
        List<SeedBomLine>? Lines);

    private sealed record SeedBomLine(
        string ComponentId,
        string Description,
        decimal Quantity,
        string Unit);
}
