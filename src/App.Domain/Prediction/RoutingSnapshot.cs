namespace App.Domain.Prediction;

public sealed record RoutingOperation(
    string OperationReference,
    int Sequence,
    string WorkCenterReference,
    long StandardDurationMinutes,
    IReadOnlyList<string> PredecessorOperations);

public sealed record RoutingSnapshot(IReadOnlyList<RoutingOperation> Operations);
