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
    private readonly IReadOnlyList<MockErpStockLevel> _stockLevels;
    private readonly IReadOnlyList<MockErpOpenPurchaseOrder> _openPurchaseOrders;
    private readonly IReadOnlyList<MockErpWorkOrder> _workOrders;
    private readonly IReadOnlyList<MockErpWorkCenter> _workCenters;
    private readonly IReadOnlyList<MockErpWorkingShift> _shifts;
    private readonly IReadOnlyList<MockErpHoliday> _holidays;
    private readonly IReadOnlyList<MockErpPlannedDowntime> _plannedDowntimes;
    private readonly IReadOnlyList<MockErpShippingDuration> _shippingDurations;

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

        if (seed.Orders is null || seed.Products is null || seed.Boms is null ||
            seed.StockLevels is null || seed.OpenPurchaseOrders is null ||
            seed.WorkOrders is null || seed.CapacityCalendar is null ||
            seed.ShippingDurations is null)
        {
            throw new InvalidOperationException(
                "Mock ERP seed must contain all required resource collections.");
        }

        EnsureUnique(seed.Orders.Select(order => order.Id), "order");
        EnsureUnique(seed.Products.Select(product => product.Id), "product");
        EnsureUnique(seed.Boms.Select(bom => bom.ProductId), "BOM product");
        EnsureUnique(
            seed.CapacityCalendar.WorkCenters.Select(workCenter => workCenter.WorkCenterRef),
            "work center");
        EnsureValidWorkOrderOperations(seed.WorkOrders, seed.CapacityCalendar.WorkCenters);

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

        _stockLevels = ReadOnly(seed.StockLevels);
        _openPurchaseOrders = ReadOnly(seed.OpenPurchaseOrders);
        _workOrders = Array.AsReadOnly(seed.WorkOrders
            .Select(workOrder => workOrder with
            {
                Operations = Array.AsReadOnly(workOrder.Operations
                    .Select(operation => operation with
                    {
                        PredecessorOperationReferences =
                            Array.AsReadOnly(operation.PredecessorOperationReferences.ToArray())
                    })
                    .ToArray())
            })
            .ToArray());
        _workCenters = ReadOnly(seed.CapacityCalendar.WorkCenters);
        _shifts = ReadOnly(seed.CapacityCalendar.Shifts);
        _holidays = ReadOnly(seed.CapacityCalendar.Holidays);
        _plannedDowntimes = ReadOnly(seed.CapacityCalendar.PlannedDowntimes);
        _shippingDurations = ReadOnly(seed.ShippingDurations);
    }

    public IReadOnlyList<MockErpOrder> GetOrders() => _orders;

    public MockErpOrder? GetOrder(string id) =>
        _ordersById.GetValueOrDefault(id);

    public MockErpProduct? GetProduct(string id) =>
        _productsById.GetValueOrDefault(id);

    public IReadOnlyList<MockErpBomLine> GetProductBom(string productId) =>
        _bomsByProductId.GetValueOrDefault(productId) ?? Array.Empty<MockErpBomLine>();

    public IReadOnlyList<MockErpStockLevel> GetStockLevels(IEnumerable<string> productReferences) =>
        FilterByReferences(_stockLevels, productReferences, item => item.ProductReference);

    public IReadOnlyList<MockErpOpenPurchaseOrder> GetOpenPurchaseOrders(
        IEnumerable<string> productReferences) =>
        FilterByReferences(_openPurchaseOrders, productReferences, item => item.ProductReference);

    public IReadOnlyList<MockErpWorkOrder> GetWorkOrders(
        string orderReference,
        IEnumerable<string> productReferences)
    {
        var references = ToReferenceSet(productReferences);
        return Array.AsReadOnly(_workOrders
            .Where(item =>
                string.Equals(item.OrderReference, orderReference, StringComparison.Ordinal) &&
                references.Contains(item.ProductReference))
            .ToArray());
    }

    public MockErpCapacityAndCalendar GetCapacityAndCalendar(
        IEnumerable<string> workCenterReferences,
        DateTimeOffset rangeStart,
        DateTimeOffset rangeEnd)
    {
        var references = ToReferenceSet(workCenterReferences);
        var startDate = DateOnly.FromDateTime(rangeStart.Date);
        var endDate = DateOnly.FromDateTime(rangeEnd.Date);

        return new MockErpCapacityAndCalendar(
            rangeStart,
            rangeEnd,
            Filter(_workCenters, item => references.Contains(item.WorkCenterRef)),
            Filter(_shifts, item =>
                references.Contains(item.WorkCenterReference) &&
                item.End >= rangeStart && item.Start <= rangeEnd),
            Filter(_holidays, item =>
                (item.WorkCenterReference is null || references.Contains(item.WorkCenterReference)) &&
                item.Date >= startDate && item.Date <= endDate),
            Filter(_plannedDowntimes, item =>
                references.Contains(item.WorkCenterReference) &&
                item.End >= rangeStart && item.Start <= rangeEnd));
    }

    public MockErpShippingDuration? GetShippingDuration(
        string originReference,
        string destinationReference,
        string shippingProfileReference) =>
        _shippingDurations.FirstOrDefault(item =>
            string.Equals(item.OriginReference, originReference, StringComparison.Ordinal) &&
            string.Equals(item.DestinationReference, destinationReference, StringComparison.Ordinal) &&
            string.Equals(
                item.ShippingProfileReference,
                shippingProfileReference,
                StringComparison.Ordinal));

    private static IReadOnlyList<T> ReadOnly<T>(IEnumerable<T> items) =>
        Array.AsReadOnly(items.ToArray());

    private static IReadOnlyList<T> Filter<T>(
        IEnumerable<T> items,
        Func<T, bool> predicate) =>
        Array.AsReadOnly(items.Where(predicate).ToArray());

    private static IReadOnlyList<T> FilterByReferences<T>(
        IEnumerable<T> items,
        IEnumerable<string> references,
        Func<T, string> referenceSelector)
    {
        var referenceSet = ToReferenceSet(references);
        return Filter(items, item => referenceSet.Contains(referenceSelector(item)));
    }

    private static HashSet<string> ToReferenceSet(IEnumerable<string> references) =>
        new(references, StringComparer.Ordinal);

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

    // Each work order's Operations list is the routing-scope boundary (T-359): operation
    // reference/sequence uniqueness and predecessor references are validated per work
    // order, not globally, since a routing is represented as one work order's operations.
    private static void EnsureValidWorkOrderOperations(
        IEnumerable<MockErpWorkOrder> workOrders,
        IEnumerable<MockErpWorkCenter> workCenters)
    {
        var knownWorkCenters = new HashSet<string>(
            workCenters.Select(workCenter => workCenter.WorkCenterRef),
            StringComparer.Ordinal);

        foreach (var workOrder in workOrders)
        {
            var operations = workOrder.Operations;

            EnsureUnique(
                operations.Select(operation => operation.OperationReference),
                $"operation reference in work order '{workOrder.WorkOrderReference}'");
            EnsureUnique(
                operations.Select(operation => operation.OperationSequence.ToString()),
                $"operation sequence in work order '{workOrder.WorkOrderReference}'");

            var knownOperationReferences = new HashSet<string>(
                operations.Select(operation => operation.OperationReference),
                StringComparer.Ordinal);

            foreach (var operation in operations)
            {
                if (operation.StandardDurationMinutes <= 0)
                {
                    throw new InvalidOperationException(
                        $"Work order '{workOrder.WorkOrderReference}' operation " +
                        $"'{operation.OperationReference}' has an invalid StandardDurationMinutes " +
                        $"({operation.StandardDurationMinutes}); it must be positive.");
                }

                if (operation.OperationSequence <= 0)
                {
                    throw new InvalidOperationException(
                        $"Work order '{workOrder.WorkOrderReference}' operation " +
                        $"'{operation.OperationReference}' has an invalid OperationSequence " +
                        $"({operation.OperationSequence}); it must be positive.");
                }

                if (!knownWorkCenters.Contains(operation.WorkCenterReference))
                {
                    throw new InvalidOperationException(
                        $"Work order '{workOrder.WorkOrderReference}' operation " +
                        $"'{operation.OperationReference}' references unknown work center " +
                        $"'{operation.WorkCenterReference}'.");
                }

                foreach (var predecessor in operation.PredecessorOperationReferences)
                {
                    if (string.Equals(predecessor, operation.OperationReference, StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException(
                            $"Work order '{workOrder.WorkOrderReference}' operation " +
                            $"'{operation.OperationReference}' cannot list itself as a predecessor.");
                    }

                    if (!knownOperationReferences.Contains(predecessor))
                    {
                        throw new InvalidOperationException(
                            $"Work order '{workOrder.WorkOrderReference}' operation " +
                            $"'{operation.OperationReference}' references predecessor '{predecessor}', " +
                            "which is not part of the same work order.");
                    }
                }
            }
        }
    }

    private sealed record SeedDocument(
        List<SeedOrder>? Orders,
        List<SeedProduct>? Products,
        List<SeedBom>? Boms,
        List<MockErpStockLevel>? StockLevels,
        List<MockErpOpenPurchaseOrder>? OpenPurchaseOrders,
        List<MockErpWorkOrder>? WorkOrders,
        SeedCapacityCalendar? CapacityCalendar,
        List<MockErpShippingDuration>? ShippingDurations);

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

    private sealed record SeedCapacityCalendar(
        List<MockErpWorkCenter> WorkCenters,
        List<MockErpWorkingShift> Shifts,
        List<MockErpHoliday> Holidays,
        List<MockErpPlannedDowntime> PlannedDowntimes);
}
