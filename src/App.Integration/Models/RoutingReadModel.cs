namespace App.Integration.Models;

internal sealed record RoutingReadModel(
    string RoutingReference,
    IReadOnlyList<OperationReadModel> Operations);

internal sealed record OperationReadModel(
    string OperationReference,
    int OperationSequence,
    string WorkCenterReference,
    long StandardDurationMinutes,
    IReadOnlyList<string> PredecessorOperationReferences);
