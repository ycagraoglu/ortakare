using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ortakare.Api.Common;
using Ortakare.Api.Features.Photos.GetEventPhotos;

namespace Ortakare.Api.Features.Photos;

[ApiController]
[Authorize]
[Route("api/events/{eventId:guid}/photos")]
public sealed class EventPhotosController(GetEventPhotosHandler getEventPhotosHandler) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResult<GetEventPhotosResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResult), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Get(
        Guid eventId,
        [FromQuery] GetEventPhotosRequest request,
        CancellationToken cancellationToken)
    {
        var result = await getEventPhotosHandler.HandleAsync(eventId, request, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }
}