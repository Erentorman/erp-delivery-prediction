using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using MockErp.Api.Data;
using MockErp.Api.Models;

namespace MockErp.Api.Controllers;

[ApiController]
[Route("api/work-orders")]
public sealed class WorkOrdersController(MockErpDataStore dataStore) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<MockErpWorkOrder>>(StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<MockErpWorkOrder>> Get(
        [FromQuery, Required] string orderReference,
        [FromQuery, Required, MinLength(1)] string[] productReferences,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Ok(dataStore.GetWorkOrders(orderReference, productReferences));
    }
}
