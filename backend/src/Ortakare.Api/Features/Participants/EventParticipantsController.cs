using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ortakare.Api.Common;
using Ortakare.Api.Features.Participants.BlockEventParticipant;
using Ortakare.Api.Features.Participants.DeleteEventParticipant;
using Ortakare.Api.Features.Participants.GetEventParticipants;
using Ortakare.Api.Features.Participants.UnblockEventParticipant;

namespace Ortakare.Api.Features.Participants;

[ApiController]
[Authorize]
[Route("api/events/{eventId:guid}/participants")]
public sealed class EventParticipantsController(
    GetEventParticipantsHandler getEventParticipantsHandler,
    DeleteEventParticipantHandler deleteEventParticipantHandler,
    BlockEventParticipantHandler blockEventParticipantHandler,
    UnblockEventParticipantHandler unblockEventParticipantHandler) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResult<GetEventParticipantsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResult), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Get(
        Guid eventId,
        [FromQuery] GetEventParticipantsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await getEventParticipantsHandler.HandleAsync(
            eventId,
            request,
            cancellationToken);

        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("{participantId:guid}/block")]
    [ProducesResponseType(typeof(ApiResult<BlockEventParticipantResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResult), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Block(
        Guid eventId,
        Guid participantId,
        CancellationToken cancellationToken)
    {
        var result = await blockEventParticipantHandler.HandleAsync(
            eventId,
            participantId,
            cancellationToken);

        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("{participantId:guid}/unblock")]
    [ProducesResponseType(typeof(ApiResult<UnblockEventParticipantResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResult), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Unblock(
        Guid eventId,
        Guid participantId,
        CancellationToken cancellationToken)
    {
        var result = await unblockEventParticipantHandler.HandleAsync(
            eventId,
            participantId,
            cancellationToken);

        return StatusCode(result.StatusCode, result);
    }

    [HttpDelete("{participantId:guid}")]
    [ProducesResponseType(typeof(ApiResult<DeleteEventParticipantResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResult), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResult), StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Delete(
        Guid eventId,
        Guid participantId,
        CancellationToken cancellationToken)
    {
        var result = await deleteEventParticipantHandler.HandleAsync(
            eventId,
            participantId,
            cancellationToken);

        return StatusCode(result.StatusCode, result);
    }
}
