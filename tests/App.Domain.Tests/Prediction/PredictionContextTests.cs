using App.Domain.Prediction;

namespace App.Domain.Tests.Prediction;

public class PredictionContextTests
{
    [Fact]
    public void Constructor_WithValidArguments_ExposesProvidedSnapshots()
    {
        var orderInput = new OrderInput();
        var materialSnapshot = new MaterialSnapshot();
        var capacitySnapshot = new CapacitySnapshot();
        var calendarSnapshot = new CalendarSnapshot();
        var operation = new Operation();

        var context = new PredictionContext(
            orderInput,
            materialSnapshot,
            capacitySnapshot,
            calendarSnapshot,
            new[] { operation });

        Assert.Same(orderInput, context.OrderInput);
        Assert.Same(materialSnapshot, context.MaterialSnapshot);
        Assert.Same(capacitySnapshot, context.CapacitySnapshot);
        Assert.Same(calendarSnapshot, context.CalendarSnapshot);
        Assert.Single(context.Operations);
        Assert.Same(operation, context.Operations[0]);
    }

    [Fact]
    public void Constructor_WithNullOrderInput_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new PredictionContext(
            null!,
            new MaterialSnapshot(),
            new CapacitySnapshot(),
            new CalendarSnapshot(),
            Array.Empty<Operation>()));
    }

    [Fact]
    public void Constructor_WithNullMaterialSnapshot_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new PredictionContext(
            new OrderInput(),
            null!,
            new CapacitySnapshot(),
            new CalendarSnapshot(),
            Array.Empty<Operation>()));
    }

    [Fact]
    public void Constructor_WithNullCapacitySnapshot_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new PredictionContext(
            new OrderInput(),
            new MaterialSnapshot(),
            null!,
            new CalendarSnapshot(),
            Array.Empty<Operation>()));
    }

    [Fact]
    public void Constructor_WithNullCalendarSnapshot_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new PredictionContext(
            new OrderInput(),
            new MaterialSnapshot(),
            new CapacitySnapshot(),
            null!,
            Array.Empty<Operation>()));
    }

    [Fact]
    public void Constructor_WithNullOperations_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new PredictionContext(
            new OrderInput(),
            new MaterialSnapshot(),
            new CapacitySnapshot(),
            new CalendarSnapshot(),
            null!));
    }

    [Fact]
    public void AddOperation_WithValidOperation_AppendsToOperations()
    {
        var context = new PredictionContext(
            new OrderInput(),
            new MaterialSnapshot(),
            new CapacitySnapshot(),
            new CalendarSnapshot(),
            Array.Empty<Operation>());

        var operation = new Operation();
        context.AddOperation(operation);

        Assert.Single(context.Operations);
        Assert.Same(operation, context.Operations[0]);
    }

    [Fact]
    public void AddOperation_WithNullOperation_ThrowsArgumentNullException()
    {
        var context = new PredictionContext(
            new OrderInput(),
            new MaterialSnapshot(),
            new CapacitySnapshot(),
            new CalendarSnapshot(),
            Array.Empty<Operation>());

        Assert.Throws<ArgumentNullException>(() => context.AddOperation(null!));
    }
}
