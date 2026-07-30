using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using MockErp.Api.Data;
using MockErp.Api.Models;

namespace MockErp.Api.Controllers;

[ApiController]
[Route("api/capacity-calendar")]
public sealed class CapacityCalendarController(MockErpDataStore dataStore) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<MockErpCapacityAndCalendar>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public ActionResult<MockErpCapacityAndCalendar> Get(
        [FromQuery, Required, MinLength(1)] string[] workCenterReferences,
        [FromQuery] DateTimeOffset rangeStart,
        [FromQuery] DateTimeOffset rangeEnd,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (rangeEnd < rangeStart)
        {
            ModelState.AddModelError(
                nameof(rangeEnd),
                "rangeEnd must not be before rangeStart.");
            return ValidationProblem(ModelState);
        }

        return Ok(dataStore.GetCapacityAndCalendar(
            workCenterReferences,
            rangeStart,
            rangeEnd));
    }
}
