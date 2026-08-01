using Microsoft.AspNetCore.Mvc;
using MockErp.Api.Data;
using MockErp.Api.Models;

namespace MockErp.Api.Controllers;

[ApiController]
[Route("api/orders")]
public sealed class OrdersController(MockErpDataStore dataStore) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<MockErpOrder>>(StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<MockErpOrder>> GetAll() =>
        Ok(dataStore.GetOrders());

    [HttpGet("{id}")]
    [ProducesResponseType<MockErpOrder>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<MockErpOrder> GetById(string id)
    {
        var order = dataStore.GetOrder(id);
        return order is null ? NotFound() : Ok(order);
    }
}
