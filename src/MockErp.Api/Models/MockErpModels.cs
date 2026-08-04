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
    MockErpRouting Routing);

// Pending ERP Decision: The authoritative ERP source and final field ownership for routing and operation master data must be confirmed with the ERP team.
public sealed record MockErpRouting(
    string RoutingReference,
    IReadOnlyList<MockErpOperation> Operations);

public sealed record MockErpOperation(
    string OperationReference,
    int OperationSequence,
    string WorkCenterReference,
    long StandardDurationMinutes,
    IReadOnlyList<string> PredecessorOperationReferences);

public sealed record MockErpCapacityAndCalendar(
    DateTimeOffset RangeStart,
    DateTimeOffset RangeEnd,
    IReadOnlyList<MockErpWorkCenter> WorkCenters,
    IReadOnlyList<MockErpWorkingShift> Shifts,
    IReadOnlyList<MockErpHoliday> Holidays,
    IReadOnlyList<MockErpPlannedDowntime> PlannedDowntimes);

public sealed record MockErpWorkCenter(
    string WorkCenterRef,
    string Name);

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

public sealed record MockErpShippingRoute(
    string OriginReference,
    string DestinationReference,
    string ShippingProfileReference,
    long ShippingDurationMinutes);
