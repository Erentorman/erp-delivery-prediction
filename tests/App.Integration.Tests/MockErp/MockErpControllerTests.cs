using App.Application.Contracts.Erp;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc;
using MockErp.Api.Controllers;
using MockErp.Api.Data;
using MockErp.Api.Models;

namespace App.Integration.Tests.MockErp;

public sealed class MockErpControllerTests
{
    private readonly MockErpDataStore _store = new(Path.Combine(
        AppContext.BaseDirectory,
        "Data",
        "mock-erp-seed.json"));

    [Fact]
    public void GetOrdersReturnsExpectedCollection()
    {
        var result = new OrdersController(_store).GetAll();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var orders = Assert.IsAssignableFrom<IReadOnlyList<MockErpOrder>>(ok.Value);
        Assert.Equal(["ORD-1001", "ORD-1002"], orders.Select(order => order.Id));
    }

    [Theory]
    [InlineData("ORD-1001", true)]
    [InlineData("ORD-UNKNOWN", false)]
    public void GetOrderReturnsExpectedStatus(string id, bool exists)
    {
        var result = new OrdersController(_store).GetById(id);

        Assert.Equal(exists, result.Result is OkObjectResult);
        Assert.Equal(!exists, result.Result is NotFoundResult);
    }

    [Theory]
    [InlineData("PROD-BIKE-01", true)]
    [InlineData("PROD-UNKNOWN", false)]
    public void GetProductReturnsExpectedStatus(string id, bool exists)
    {
        var result = new ProductsController(_store).GetById(id);

        Assert.Equal(exists, result.Result is OkObjectResult);
        Assert.Equal(!exists, result.Result is NotFoundResult);
    }

    [Fact]
    public void GetKnownProductBomReturnsExpectedLines()
    {
        var result = new ProductsController(_store).GetBom("PROD-BIKE-01");

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var lines = Assert.IsAssignableFrom<IReadOnlyList<MockErpBomLine>>(ok.Value);
        Assert.Equal(["COMP-FRAME-01", "COMP-WHEEL-01"], lines.Select(line => line.ComponentId));
    }

    [Fact]
    public void StockEndpointReturnsMatchingAndEmptyCollections()
    {
        var controller = new StockLevelsController(_store);

        var matching = Assert.IsType<OkObjectResult>(
            controller.Get(["PROD-BIKE-01"], default).Result);
        Assert.Single(Assert.IsAssignableFrom<IReadOnlyList<MockErpStockLevel>>(matching.Value));

        var empty = Assert.IsType<OkObjectResult>(
            controller.Get(["UNKNOWN"], default).Result);
        Assert.Empty(Assert.IsAssignableFrom<IReadOnlyList<MockErpStockLevel>>(empty.Value));
    }

    [Fact]
    public void PurchaseOrderAndWorkOrderEndpointsReturnExpectedNestedData()
    {
        var purchaseResult = Assert.IsType<OkObjectResult>(
            new OpenPurchaseOrdersController(_store).Get(["PROD-DESK-01"], default).Result);
        Assert.Equal(
            "PO-2002",
            Assert.Single(
                Assert.IsAssignableFrom<IReadOnlyList<MockErpOpenPurchaseOrder>>(
                    purchaseResult.Value)).PurchaseOrderReference);

        var workResult = Assert.IsType<OkObjectResult>(
            new WorkOrdersController(_store)
                .Get("ORD-1001", ["PROD-BIKE-01"], default).Result);
        var workOrder = Assert.Single(
            Assert.IsAssignableFrom<IReadOnlyList<MockErpWorkOrder>>(workResult.Value));
        Assert.Equal(2, workOrder.Operations.Count);
        Assert.Equal(["WO-3001-OP10"], workOrder.Operations[1].PredecessorOperationReferences);
    }

    [Fact]
    public void CapacityEndpointReturnsFilteredDataAndRejectsInvalidRange()
    {
        var controller = new CapacityCalendarController(_store);
        var start = DateTimeOffset.Parse("2026-08-03T00:00:00+03:00");
        var end = DateTimeOffset.Parse("2026-08-04T23:59:59+03:00");

        var valid = Assert.IsType<OkObjectResult>(
            controller.Get(["WC-QUALITY-01"], start, end, default).Result);
        var calendar = Assert.IsType<MockErpCapacityAndCalendar>(valid.Value);
        Assert.Single(calendar.WorkCenters);
        Assert.All(
            calendar.Shifts,
            shift => Assert.Equal("WC-QUALITY-01", shift.WorkCenterReference));

        Assert.IsType<ObjectResult>(
            controller.Get(["WC-QUALITY-01"], end, start, default).Result);
        Assert.False(controller.ModelState.IsValid);
    }

    [Fact]
    public void ShippingEndpointReturnsKnownRouteAndNotFoundForUnknownRoute()
    {
        var controller = new ShippingDurationsController(_store);

        var known = Assert.IsType<OkObjectResult>(
            controller.Get("WH-IST-01", "CUSTOMER-ANK-01", "STANDARD", default).Result);
        Assert.Equal(720, Assert.IsType<MockErpShippingDuration>(known.Value).ShippingDurationMinutes);
        Assert.IsType<NotFoundResult>(
            controller.Get("UNKNOWN", "UNKNOWN", "UNKNOWN", default).Result);
    }

    [Theory]
    [InlineData(typeof(StockLevelsController), "api/stock-levels")]
    [InlineData(typeof(OpenPurchaseOrdersController), "api/open-purchase-orders")]
    [InlineData(typeof(WorkOrdersController), "api/work-orders")]
    [InlineData(typeof(CapacityCalendarController), "api/capacity-calendar")]
    [InlineData(typeof(ShippingDurationsController), "api/shipping-durations")]
    public void NewControllersExposeOnlyGetActions(Type controllerType, string expectedRoute)
    {
        var route = Assert.Single(
            controllerType.GetCustomAttributes(typeof(RouteAttribute), false)
                .Cast<RouteAttribute>());
        Assert.Equal(expectedRoute, route.Template);

        var actions = controllerType.GetMethods()
            .Where(method => method.DeclaringType == controllerType)
            .ToArray();
        Assert.Single(actions);
        var httpMethodAttributes = actions[0].GetCustomAttributes(true)
            .OfType<HttpMethodAttribute>()
            .ToArray();
        var get = Assert.Single(httpMethodAttributes);
        Assert.Equal(["GET"], get.HttpMethods);
    }

    [Theory]
    [InlineData(typeof(MockErpStockLevel), typeof(StockLevelReadDto))]
    [InlineData(typeof(MockErpOpenPurchaseOrder), typeof(OpenPurchaseOrderReadDto))]
    [InlineData(typeof(MockErpWorkOrderOperation), typeof(WorkOrderOperationReadDto))]
    [InlineData(typeof(MockErpWorkingShift), typeof(WorkingShiftReadDto))]
    [InlineData(typeof(MockErpHoliday), typeof(HolidayReadDto))]
    [InlineData(typeof(MockErpPlannedDowntime), typeof(PlannedDowntimeReadDto))]
    [InlineData(typeof(MockErpShippingDuration), typeof(ShippingDurationReadDto))]
    public void ResponseModelsContainApplicationReadDtoProperties(
        Type responseType,
        Type applicationDtoType)
    {
        var responseProperties = responseType.GetProperties()
            .ToDictionary(property => property.Name, property => property.PropertyType);

        foreach (var expected in applicationDtoType.GetProperties())
        {
            Assert.True(
                responseProperties.TryGetValue(expected.Name, out var actualType),
                $"{responseType.Name} is missing {expected.Name}.");
            Assert.Equal(expected.PropertyType, actualType);
        }
    }

    [Fact]
    public void AggregateResponseModelsContainNestedApplicationReadDtoInformation()
    {
        Assert.Equal(
            typeof(IReadOnlyList<MockErpWorkOrderOperation>),
            typeof(MockErpWorkOrder).GetProperty(nameof(MockErpWorkOrder.Operations))?.PropertyType);

        var capacityProperties = typeof(MockErpCapacityAndCalendar).GetProperties()
            .Select(property => property.Name);
        Assert.Equal(
            typeof(CapacityAndCalendarReadDto).GetProperties().Select(property => property.Name),
            capacityProperties);
    }
}
