using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ortakare.Api.Common;
using Ortakare.Api.Features.Events.CreateEvent;
using Ortakare.Api.Features.Events.GetMyEvents;

namespace Ortakare.Api.Features.Events;

[ApiController]
[Authorize]
[Route("api/events")]
public sealed class EventsController(
    CreateEventHandler createEventHandler,
    GetMyEventsHandler getMyEventsHandler) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(ApiResult<CreateEventResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Create(
        [FromBody] CreateEventRequest request,
        CancellationToken cancellationToken)
    {
        var result = await createEventHandler.HandleAsync(request, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResult<GetMyEventsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyEvents(
        [FromQuery] GetMyEventsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await getMyEventsHandler.HandleAsync(request, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }
}
