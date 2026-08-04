namespace App.Application.Contracts.Erp;

public sealed record OrderReadDto(
    string OrderReference,
    DateTimeOffset RequestedDeliveryDateTime,
    string? Priority,
    string? PlanningStatus);

public sealed record OrderItemReadDto(
    string OrderReference,
    string LineReference,
    string ProductReference,
    decimal OrderedQuantity,
    string UnitOfMeasure);

public sealed record ProductReadDto(
    string ProductReference,
    string? PlanningClassification,
    string UnitOfMeasure);

public sealed record BomItemReadDto(
    string ParentProductReference,
    string ComponentProductReference,
    decimal RequiredQuantityPerParentUnit,
    string UnitOfMeasure,
    string? LineReference);

public sealed record StockLevelReadDto(
    string ProductReference,
    string? LocationReference,
    decimal OnHandQuantity,
    decimal ReservedQuantity,
    decimal AvailableQuantity);

public sealed record OpenPurchaseOrderReadDto(
    string PurchaseOrderReference,
    string ProductReference,
    decimal OpenQuantity,
    DateTimeOffset ExpectedAvailabilityDateTime,
    long? SupplierLeadTimeMinutes,
    string Status);

public sealed record WorkOrderReadDto(
    string WorkOrderReference,
    string? OrderReference,
    string ProductReference,
    string Status,
    IReadOnlyList<WorkOrderOperationReadDto> Operations);

public sealed record WorkOrderOperationReadDto(
    string OperationReference,
    int OperationSequence,
    string WorkCenterReference,
    long StandardDurationMinutes,
    long? RemainingDurationMinutes,
    string Status,
    IReadOnlyList<string> PredecessorOperationReferences);

public sealed record CapacityAndCalendarReadDto(
    DateTimeOffset RangeStart,
    DateTimeOffset RangeEnd,
    IReadOnlyList<WorkCenterReadModel> WorkCenters,
    IReadOnlyList<WorkingShiftReadDto> Shifts,
    IReadOnlyList<HolidayReadDto> Holidays,
    IReadOnlyList<PlannedDowntimeReadDto> PlannedDowntimes);

public sealed record WorkCenterReadModel(
    string WorkCenterRef,
    string Name,
    int MachineCount,
    string? DefaultShiftRef);

public sealed record WorkingShiftReadDto(
    string WorkCenterReference,
    DateTimeOffset Start,
    DateTimeOffset End);

public sealed record HolidayReadDto(
    DateOnly Date,
    string? WorkCenterReference);

public sealed record PlannedDowntimeReadDto(
    string WorkCenterReference,
    DateTimeOffset Start,
    DateTimeOffset End,
    long PlannedDowntimeMinutes);

public sealed record ShippingDurationReadDto(
    string? OriginReference,
    string? DestinationReference,
    string? ShippingProfileReference,
    string? RoutingReference,
    long ShippingDurationMinutes);
