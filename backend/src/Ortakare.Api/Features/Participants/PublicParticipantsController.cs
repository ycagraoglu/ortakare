using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ortakare.Api.Common;
using Ortakare.Api.Features.Participants.JoinEvent;

namespace Ortakare.Api.Features.Participants;

[ApiController]
[AllowAnonymous]
[Route("api/public/events/{galleryToken}/participants")]
public sealed class PublicParticipantsController(JoinEventHandler joinEventHandler) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(ApiResult<JoinEventResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResult), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResult), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Join(
        string galleryToken,
        [FromBody] JoinEventRequest request,
        CancellationToken cancellationToken)
    {
        var result = await joinEventHandler.HandleAsync(
            galleryToken,
            request,
            cancellationToken);

        return StatusCode(result.StatusCode, result);
    }
}
