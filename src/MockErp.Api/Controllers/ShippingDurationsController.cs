using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using MockErp.Api.Data;
using MockErp.Api.Models;

namespace MockErp.Api.Controllers;

[ApiController]
[Route("api/shipping-durations")]
public sealed class ShippingDurationsController(MockErpDataStore dataStore) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<MockErpShippingRoute>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<MockErpShippingRoute> Get(
        [FromQuery, Required] string originReference,
        [FromQuery, Required] string destinationReference,
        [FromQuery, Required] string shippingProfileReference,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var duration = dataStore.GetShippingDuration(
            originReference,
            destinationReference,
            shippingProfileReference);
        return duration is null ? NotFound() : Ok(duration);
    }
}
