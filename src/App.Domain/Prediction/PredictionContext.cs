namespace App.Domain.Prediction;

/// <summary>
/// Provider-independent input snapshot consumed and enriched by the Step-Based Pipeline
/// (SAD §9.4, §9.1). Instances are mutable: steps receive this same instance and enrich
/// it in place via <see cref="AddOperation"/> rather than replacing it.
/// </summary>
public sealed record PredictionContext(
    OrderInput OrderInput,
    MaterialSnapshot MaterialSnapshot,
    RoutingSnapshot RoutingSnapshot,
    CapacitySnapshot CapacitySnapshot,
    CalendarSnapshot CalendarSnapshot,
    ShippingSnapshot ShippingSnapshot)
{
    private readonly List<Operation> _operations = new();

    public IReadOnlyList<Operation> Operations => _operations;

    public void AddOperation(Operation operation)
    {
        ArgumentNullException.ThrowIfNull(operation);
        _operations.Add(operation);
    }
}
