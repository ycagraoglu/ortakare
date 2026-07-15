using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ortakare.Api.Common;
using Ortakare.Api.Features.Events.CloseEvent;
using Ortakare.Api.Features.Events.CreateEvent;
using Ortakare.Api.Features.Events.GetEvent;
using Ortakare.Api.Features.Events.GetMyEvents;
using Ortakare.Api.Features.Events.ReopenEvent;
using Ortakare.Api.Features.Events.UpdateEvent;

namespace Ortakare.Api.Features.Events;

[ApiController]
[Authorize]
[Route("api/events")]
public sealed class EventsController(
    CreateEventHandler createEventHandler,
    GetMyEventsHandler getMyEventsHandler,
    GetEventHandler getEventHandler,
    UpdateEventHandler updateEventHandler,
    CloseEventHandler closeEventHandler,
    ReopenEventHandler reopenEventHandler) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(ApiResult<CreateEventResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Create([FromBody] CreateEventRequest request, CancellationToken cancellationToken)
    {
        var result = await createEventHandler.HandleAsync(request, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResult<GetMyEventsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyEvents([FromQuery] GetMyEventsRequest request, CancellationToken cancellationToken)
    {
        var result = await getMyEventsHandler.HandleAsync(request, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("{eventId:guid}")]
    [ProducesResponseType(typeof(ApiResult<GetEventResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResult), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetEvent(Guid eventId, CancellationToken cancellationToken)
    {
        var result = await getEventHandler.HandleAsync(eventId, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("{eventId:guid}")]
    [ProducesResponseType(typeof(ApiResult<UpdateEventResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResult), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Update(Guid eventId, [FromBody] UpdateEventRequest request, CancellationToken cancellationToken)
    {
        var result = await updateEventHandler.HandleAsync(eventId, request, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("{eventId:guid}/close")]
    [ProducesResponseType(typeof(ApiResult<CloseEventResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResult), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Close(Guid eventId, CancellationToken cancellationToken)
    {
        var result = await closeEventHandler.HandleAsync(eventId, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("{eventId:guid}/reopen")]
    [ProducesResponseType(typeof(ApiResult<ReopenEventResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResult), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Reopen(Guid eventId, CancellationToken cancellationToken)
    {
        var result = await reopenEventHandler.HandleAsync(eventId, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }
}
