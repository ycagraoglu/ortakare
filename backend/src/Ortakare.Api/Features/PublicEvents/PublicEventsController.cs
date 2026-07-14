using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ortakare.Api.Common;
using Ortakare.Api.Features.PublicEvents.GetPublicEvent;

namespace Ortakare.Api.Features.PublicEvents;

[ApiController]
[AllowAnonymous]
[Route("api/public/events")]
public sealed class PublicEventsController(
    GetPublicEventHandler getPublicEventHandler) : ControllerBase
{
    [HttpGet("{galleryToken}")]
    [ProducesResponseType(typeof(ApiResult<GetPublicEventResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResult), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPublicEvent(
        string galleryToken,
        CancellationToken cancellationToken)
    {
        var result = await getPublicEventHandler.HandleAsync(
            galleryToken,
            cancellationToken);

        return StatusCode(result.StatusCode, result);
    }
}
