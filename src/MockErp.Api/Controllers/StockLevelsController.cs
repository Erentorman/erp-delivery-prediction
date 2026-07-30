using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using MockErp.Api.Data;
using MockErp.Api.Models;

namespace MockErp.Api.Controllers;

[ApiController]
[Route("api/stock-levels")]
public sealed class StockLevelsController(MockErpDataStore dataStore) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<MockErpStockLevel>>(StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<MockErpStockLevel>> Get(
        [FromQuery, Required, MinLength(1)] string[] productReferences,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Ok(dataStore.GetStockLevels(productReferences));
    }
}
