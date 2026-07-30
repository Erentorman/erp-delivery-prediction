using App.Domain.Prediction;

namespace App.Domain.Tests.Prediction;

/// <summary>
/// Minimal, test-only IPredictionStep implementation used to verify the contract.
/// Not a real pipeline step — it exists solely to exercise Execute/PredictionContext.
/// </summary>
internal sealed class ExamplePredictionStep : IPredictionStep
{
    private readonly Operation _operationToAdd;

    public ExamplePredictionStep(Operation operationToAdd)
    {
        ArgumentNullException.ThrowIfNull(operationToAdd);
        _operationToAdd = operationToAdd;
    }

    public void Execute(PredictionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.AddOperation(_operationToAdd);
    }
}
