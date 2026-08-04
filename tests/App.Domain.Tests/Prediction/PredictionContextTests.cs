using App.Domain.Prediction;

namespace App.Domain.Tests.Prediction;

public class PredictionContextTests
{
    [Fact]
    public void Constructor_WithValidArguments_ExposesProvidedSnapshots()
    {
        var orderInput = new OrderInput("ORD-1", "PROD-1", 10, DateTimeOffset.UtcNow);
        var materialSnapshot = new MaterialSnapshot(Array.Empty<MaterialProduct>(), Array.Empty<MaterialBomItem>(), Array.Empty<MaterialStock>(), Array.Empty<MaterialPurchaseOrder>());
        var routingSnapshot = new RoutingSnapshot(Array.Empty<RoutingOperation>());
        var capacitySnapshot = new CapacitySnapshot();
        var calendarSnapshot = new CalendarSnapshot();
        var shippingSnapshot = new ShippingSnapshot();

        var context = new PredictionContext(
            orderInput,
            materialSnapshot,
            routingSnapshot,
            capacitySnapshot,
            calendarSnapshot,
            shippingSnapshot);

        Assert.Same(orderInput, context.OrderInput);
        Assert.Same(materialSnapshot, context.MaterialSnapshot);
        Assert.Same(routingSnapshot, context.RoutingSnapshot);
        Assert.Same(capacitySnapshot, context.CapacitySnapshot);
        Assert.Same(calendarSnapshot, context.CalendarSnapshot);
        Assert.Same(shippingSnapshot, context.ShippingSnapshot);
        Assert.Empty(context.Operations);
    }

    [Fact]
    public void AddOperation_WithValidOperation_AppendsToOperations()
    {
        var context = new PredictionContext(
            new OrderInput("ORD-1", "PROD-1", 10, DateTimeOffset.UtcNow),
            new MaterialSnapshot(Array.Empty<MaterialProduct>(), Array.Empty<MaterialBomItem>(), Array.Empty<MaterialStock>(), Array.Empty<MaterialPurchaseOrder>()),
            new RoutingSnapshot(Array.Empty<RoutingOperation>()),
            new CapacitySnapshot(),
            new CalendarSnapshot(),
            new ShippingSnapshot());

        var operation = new Operation();
        context.AddOperation(operation);

        Assert.Single(context.Operations);
        Assert.Same(operation, context.Operations[0]);
    }

    [Fact]
    public void AddOperation_WithNullOperation_ThrowsArgumentNullException()
    {
        var context = new PredictionContext(
            new OrderInput("ORD-1", "PROD-1", 10, DateTimeOffset.UtcNow),
            new MaterialSnapshot(Array.Empty<MaterialProduct>(), Array.Empty<MaterialBomItem>(), Array.Empty<MaterialStock>(), Array.Empty<MaterialPurchaseOrder>()),
            new RoutingSnapshot(Array.Empty<RoutingOperation>()),
            new CapacitySnapshot(),
            new CalendarSnapshot(),
            new ShippingSnapshot());

        Assert.Throws<ArgumentNullException>(() => context.AddOperation(null!));
    }
}
