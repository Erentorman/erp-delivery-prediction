namespace MockErp.Api.Models;

public sealed record MockErpOrder(
    string Id,
    string ProductId,
    int Quantity,
    DateOnly RequestedDeliveryDate);

public sealed record MockErpProduct(
    string Id,
    string Name,
    string Unit);

public sealed record MockErpBomLine(
    string ComponentId,
    string Description,
    decimal Quantity,
    string Unit);

public sealed record MockErpStockLevel(
    string ProductReference,
    string? LocationReference,
    decimal OnHandQuantity,
    decimal ReservedQuantity,
    decimal AvailableQuantity);

public sealed record MockErpOpenPurchaseOrder(
    string PurchaseOrderReference,
    string ProductReference,
    decimal OpenQuantity,
    DateTimeOffset ExpectedAvailabilityDateTime,
    long? SupplierLeadTimeMinutes,
    string Status);

public sealed record MockErpWorkOrder(
    string WorkOrderReference,
    string? OrderReference,
    string ProductReference,
    string Status,
    IReadOnlyList<MockErpWorkOrderOperation> Operations);

public sealed record MockErpWorkOrderOperation(
    string OperationReference,
    int OperationSequence,
    string WorkCenterReference,
    long StandardDurationMinutes,
    long? RemainingDurationMinutes,
    string Status,
    IReadOnlyList<string> PredecessorOperationReferences);

public sealed record MockErpCapacityAndCalendar(
    DateTimeOffset RangeStart,
    DateTimeOffset RangeEnd,
    IReadOnlyList<MockErpWorkCenterCapacity> WorkCenters,
    IReadOnlyList<MockErpWorkingShift> Shifts,
    IReadOnlyList<MockErpHoliday> Holidays,
    IReadOnlyList<MockErpPlannedDowntime> PlannedDowntimes);

public sealed record MockErpWorkCenterCapacity(
    string WorkCenterReference,
    long CapacityMinutes,
    long AvailableCapacityMinutes,
    long CurrentLoadMinutes,
    string Name,
    int MachineCount,
    string? DefaultShiftReference);

public sealed record MockErpWorkingShift(
    string WorkCenterReference,
    DateTimeOffset Start,
    DateTimeOffset End);

public sealed record MockErpHoliday(
    DateOnly Date,
    string? WorkCenterReference);

public sealed record MockErpPlannedDowntime(
    string WorkCenterReference,
    DateTimeOffset Start,
    DateTimeOffset End,
    long PlannedDowntimeMinutes);

public sealed record MockErpShippingDuration(
    string? OriginReference,
    string? DestinationReference,
    string? ShippingProfileReference,
    string? RoutingReference,
    long ShippingDurationMinutes);
