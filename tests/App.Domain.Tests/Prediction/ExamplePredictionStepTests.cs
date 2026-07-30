using App.Domain.Prediction;

namespace App.Domain.Tests.Prediction;

public class ExamplePredictionStepTests
{
    [Fact]
    public void Execute_EnrichesTheSamePredictionContextInstance()
    {
        var context = new PredictionContext(
            new OrderInput(),
            new MaterialSnapshot(),
            new CapacitySnapshot(),
            new CalendarSnapshot(),
            Array.Empty<Operation>());
        var contextReference = context;
        var operation = new Operation();
        IPredictionStep step = new ExamplePredictionStep(operation);

        step.Execute(context);

        Assert.Same(contextReference, context);
        Assert.Single(context.Operations);
        Assert.Same(operation, context.Operations[0]);
    }

    [Fact]
    public void Execute_CalledTwice_AccumulatesEnrichmentsOnSameInstance()
    {
        var context = new PredictionContext(
            new OrderInput(),
            new MaterialSnapshot(),
            new CapacitySnapshot(),
            new CalendarSnapshot(),
            Array.Empty<Operation>());
        var firstOperation = new Operation();
        var secondOperation = new Operation();
        IPredictionStep firstStep = new ExamplePredictionStep(firstOperation);
        IPredictionStep secondStep = new ExamplePredictionStep(secondOperation);

        firstStep.Execute(context);
        secondStep.Execute(context);

        Assert.Equal(2, context.Operations.Count);
        Assert.Same(firstOperation, context.Operations[0]);
        Assert.Same(secondOperation, context.Operations[1]);
    }

    [Fact]
    public void Execute_WithNullContext_ThrowsArgumentNullException()
    {
        IPredictionStep step = new ExamplePredictionStep(new Operation());

        Assert.Throws<ArgumentNullException>(() => step.Execute(null!));
    }

    [Fact]
    public void Constructor_WithNullOperation_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new ExamplePredictionStep(null!));
    }
}
