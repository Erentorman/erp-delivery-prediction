using App.Application.Contracts.Erp;
using App.Application.Prediction;
using App.Domain.Prediction;

namespace App.Application.Tests.Prediction;

public class PredictionContextBuilderTests
{
    private readonly PredictionContextBuilder _builder = new();

    private ErpBatchSnapshot CreateValidSnapshot()
    {
        var order = new OrderReadDto("ORD-1", DateTimeOffset.UtcNow, null, null);
        var orderItems = new[] { new OrderItemReadDto("ORD-1", "LINE-1", "PROD-1", 10, "pcs") };
        var products = new[] { new ProductReadDto("PROD-1", null, "pcs") };
        var bomItems = new[] { new BomItemReadDto("PROD-1", "COMP-1", 2, "pcs", null) };
        
        var operations = new[] { new OperationReadDto("OP-1", 10, "WC-1", 30, Array.Empty<string>()) };
        var routing = new RoutingReadDto("ROUT-1", operations);
        var workOrders = new[] { new WorkOrderReadDto("WO-1", "ORD-1", "PROD-1", "Active", routing) };

        return new ErpBatchSnapshot(
            DateTimeOffset.UtcNow,
            order,
            orderItems,
            products,
            bomItems,
            Array.Empty<StockLevelReadDto>(),
            Array.Empty<OpenPurchaseOrderReadDto>(),
            workOrders
        );
    }

    [Fact]
    public void Build_WithValidSnapshot_ReturnsSufficientAndContext()
    {
        var snapshot = CreateValidSnapshot();

        var (status, context) = _builder.Build(snapshot);

        Assert.Equal(DataSufficiency.Sufficient, status);
        Assert.NotNull(context);
        
        Assert.Equal("ORD-1", context.OrderInput.OrderReference);
        Assert.Equal(10, context.OrderInput.Quantity);

        Assert.Single(context.MaterialSnapshot.Products);
        Assert.Single(context.MaterialSnapshot.BomItems);
        Assert.Empty(context.MaterialSnapshot.StockLevels);
        Assert.Empty(context.MaterialSnapshot.OpenPurchaseOrders);

        Assert.Single(context.RoutingSnapshot.Operations);
        Assert.Equal("OP-1", context.RoutingSnapshot.Operations[0].OperationReference);
    }

    [Fact]
    public void Build_WithZeroQuantity_ReturnsInsufficientData()
    {
        var snapshot = CreateValidSnapshot();
        var zeroQtyItems = new[] { new OrderItemReadDto("ORD-1", "LINE-1", "PROD-1", 0, "pcs") };
        var badSnapshot = snapshot with { OrderItems = zeroQtyItems };

        var (status, context) = _builder.Build(badSnapshot);

        Assert.Equal(DataSufficiency.InsufficientData, status);
        Assert.Null(context);
    }

    [Fact]
    public void Build_WithEmptyBomItems_ReturnsInsufficientData()
    {
        var snapshot = CreateValidSnapshot();
        var badSnapshot = snapshot with { BomItems = Array.Empty<BomItemReadDto>() };

        var (status, context) = _builder.Build(badSnapshot);

        Assert.Equal(DataSufficiency.InsufficientData, status);
        Assert.Null(context);
    }

    [Fact]
    public void Build_WithNoRouting_ReturnsInsufficientData()
    {
        var snapshot = CreateValidSnapshot();
        var badSnapshot = snapshot with { WorkOrders = Array.Empty<WorkOrderReadDto>() };

        var (status, context) = _builder.Build(badSnapshot);

        Assert.Equal(DataSufficiency.InsufficientData, status);
        Assert.Null(context);
    }
}
