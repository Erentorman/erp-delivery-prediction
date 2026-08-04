using App.Application.Contracts.Erp;

namespace App.Application.Tests.Erp;

public sealed class ErpReadDtoTests
{
    [Fact]
    public void WorkCenterReadDto_ContainsOnlyTheMinimumContract()
    {
        Assert.Equal(
            [nameof(WorkCenterReadDto.WorkCenterRef), nameof(WorkCenterReadDto.Name)],
            typeof(WorkCenterReadDto).GetProperties().Select(property => property.Name));
    }

    [Fact]
    public void RoutingAndOperationReadDtos_ContainOnlyTheMinimumContract()
    {
        Assert.Equal(
            [nameof(RoutingReadDto.RoutingReference), nameof(RoutingReadDto.Operations)],
            typeof(RoutingReadDto).GetProperties().Select(property => property.Name));
        Assert.Equal(
            [
                nameof(OperationReadDto.OperationReference),
                nameof(OperationReadDto.OperationSequence),
                nameof(OperationReadDto.WorkCenterReference),
                nameof(OperationReadDto.StandardDurationMinutes),
                nameof(OperationReadDto.PredecessorOperationReferences)
            ],
            typeof(OperationReadDto).GetProperties().Select(property => property.Name));
        Assert.Equal(
            typeof(long),
            typeof(OperationReadDto).GetProperty(nameof(OperationReadDto.StandardDurationMinutes))?.PropertyType);
    }

    [Fact]
    public void RepresentativeDtos_PreserveDeterministicValues()
    {
        var requestedDelivery = new DateTimeOffset(2026, 8, 10, 9, 30, 0, TimeSpan.Zero);
        var operation = new OperationReadDto(
            "OP-20",
            20,
            "WC-ASSEMBLY",
            90,
            new[] { "OP-10" });
        var routing = new RoutingReadDto(
            "ROUTE-PRODUCT-1-STD",
            new[] { operation });
        var workOrder = new WorkOrderReadDto(
            "WO-100",
            "SO-100",
            "PRODUCT-1",
            "Released",
            routing);
        var order = new OrderReadDto("SO-100", requestedDelivery, "High", "Planned");
        var item = new OrderItemReadDto("SO-100", "10", "PRODUCT-1", 12.5m, "EA");
        var shipping = new ShippingDurationReadDto(
            "WH-1",
            "ZONE-2",
            "STANDARD",
            "ROUTE-7",
            1_440);

        Assert.Equal("SO-100", order.OrderReference);
        Assert.Equal(requestedDelivery, order.RequestedDeliveryDateTime);
        Assert.Equal(12.5m, item.OrderedQuantity);
        Assert.Equal("EA", item.UnitOfMeasure);
        Assert.Same(operation, Assert.Single(workOrder.Routing.Operations));
        Assert.Equal("OP-10", Assert.Single(operation.PredecessorOperationReferences));
        Assert.Equal("ROUTE-PRODUCT-1-STD", workOrder.Routing.RoutingReference);
        Assert.Equal(1_440, shipping.ShippingDurationMinutes);
        Assert.Equal("ROUTE-7", shipping.RoutingReference);
    }

    [Fact]
    public void CapacityAndCalendarDto_PreservesCalendarInputs()
    {
        var rangeStart = new DateTimeOffset(2026, 8, 3, 0, 0, 0, TimeSpan.Zero);
        var rangeEnd = rangeStart.AddDays(7);
        var workCenter = new WorkCenterReadDto("WC-1", "Assembly Line 1");
        var shift = new WorkingShiftReadDto(
            "WC-1",
            rangeStart.AddHours(8),
            rangeStart.AddHours(16));
        var holiday = new HolidayReadDto(new DateOnly(2026, 8, 5), null);
        var downtime = new PlannedDowntimeReadDto(
            "WC-1",
            rangeStart.AddDays(1).AddHours(10),
            rangeStart.AddDays(1).AddHours(11),
            60);
        var snapshot = new CapacityAndCalendarReadDto(
            rangeStart,
            rangeEnd,
            new[] { workCenter },
            new[] { shift },
            new[] { holiday },
            new[] { downtime });

        Assert.Equal(rangeStart, snapshot.RangeStart);
        Assert.Equal(rangeEnd, snapshot.RangeEnd);
        Assert.Same(workCenter, Assert.Single(snapshot.WorkCenters));
        Assert.Same(shift, Assert.Single(snapshot.Shifts));
        Assert.Same(holiday, Assert.Single(snapshot.Holidays));
        Assert.Same(downtime, Assert.Single(snapshot.PlannedDowntimes));
        Assert.Equal(60, downtime.PlannedDowntimeMinutes);
    }
}
