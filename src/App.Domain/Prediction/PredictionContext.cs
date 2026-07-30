namespace App.Domain.Prediction;

/// <summary>
/// Provider-independent input snapshot consumed and enriched by the Step-Based Pipeline
/// (SAD §9.4, §9.1). Instances are mutable: steps receive this same instance and enrich
/// it in place via <see cref="AddOperation"/> rather than replacing it.
/// </summary>
public sealed class PredictionContext
{
    private readonly List<Operation> _operations;

    public OrderInput OrderInput { get; }

    public MaterialSnapshot MaterialSnapshot { get; }

    public CapacitySnapshot CapacitySnapshot { get; }

    public CalendarSnapshot CalendarSnapshot { get; }

    public IReadOnlyList<Operation> Operations => _operations;

    public PredictionContext(
        OrderInput orderInput,
        MaterialSnapshot materialSnapshot,
        CapacitySnapshot capacitySnapshot,
        CalendarSnapshot calendarSnapshot,
        IEnumerable<Operation> operations)
    {
        ArgumentNullException.ThrowIfNull(orderInput);
        ArgumentNullException.ThrowIfNull(materialSnapshot);
        ArgumentNullException.ThrowIfNull(capacitySnapshot);
        ArgumentNullException.ThrowIfNull(calendarSnapshot);
        ArgumentNullException.ThrowIfNull(operations);

        OrderInput = orderInput;
        MaterialSnapshot = materialSnapshot;
        CapacitySnapshot = capacitySnapshot;
        CalendarSnapshot = calendarSnapshot;
        _operations = new List<Operation>(operations);
    }

    public void AddOperation(Operation operation)
    {
        ArgumentNullException.ThrowIfNull(operation);
        _operations.Add(operation);
    }
}
