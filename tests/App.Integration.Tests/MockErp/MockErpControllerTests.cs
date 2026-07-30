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
}
