namespace App.Integration.MockErp;

internal sealed record MockErpOrder(string Id, string ProductId, int Quantity, DateOnly RequestedDeliveryDate);
internal sealed record MockErpProduct(string Id, string Name, string Unit);
internal sealed record MockErpBomLine(string ComponentId, string Description, decimal Quantity, string Unit);
internal sealed record MockErpStockLevel(string ProductReference, string? LocationReference, decimal OnHandQuantity, decimal ReservedQuantity, decimal AvailableQuantity);
internal sealed record MockErpOpenPurchaseOrder(string PurchaseOrderReference, string ProductReference, decimal OpenQuantity, DateTimeOffset ExpectedAvailabilityDateTime, long? SupplierLeadTimeMinutes, string Status);
internal sealed record MockErpWorkOrder(string WorkOrderReference, string? OrderReference, string ProductReference, string Status, IReadOnlyList<MockErpWorkOrderOperation> Operations);
internal sealed record MockErpWorkOrderOperation(string OperationReference, int OperationSequence, string WorkCenterReference, long StandardDurationMinutes, long? RemainingDurationMinutes, string Status, IReadOnlyList<string> PredecessorOperationReferences);
internal sealed record MockErpCapacityAndCalendar(DateTimeOffset RangeStart, DateTimeOffset RangeEnd, IReadOnlyList<MockErpWorkCenterCapacity> WorkCenters, IReadOnlyList<MockErpWorkingShift> Shifts, IReadOnlyList<MockErpHoliday> Holidays, IReadOnlyList<MockErpPlannedDowntime> PlannedDowntimes);
internal sealed record MockErpWorkCenterCapacity(string WorkCenterReference, long CapacityMinutes, long AvailableCapacityMinutes, long CurrentLoadMinutes, string Name, int MachineCount, string? DefaultShiftReference);
internal sealed record MockErpWorkingShift(string WorkCenterReference, DateTimeOffset Start, DateTimeOffset End);
internal sealed record MockErpHoliday(DateOnly Date, string? WorkCenterReference);
internal sealed record MockErpPlannedDowntime(string WorkCenterReference, DateTimeOffset Start, DateTimeOffset End, long PlannedDowntimeMinutes);
internal sealed record MockErpShippingDuration(string? OriginReference, string? DestinationReference, string? ShippingProfileReference, string? RoutingReference, long ShippingDurationMinutes);
