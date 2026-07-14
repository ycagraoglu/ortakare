using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ortakare.Api.Common;
using Ortakare.Api.Features.GalleryExports.CreateGalleryExport;
using Ortakare.Api.Features.GalleryExports.GetGalleryExport;

namespace Ortakare.Api.Features.GalleryExports;

[ApiController]
[Authorize]
[Route("api/events/{eventId:guid}/exports")]
public sealed class GalleryExportsController(
    CreateGalleryExportHandler createGalleryExportHandler,
    GetGalleryExportHandler getGalleryExportHandler) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(ApiResult<CreateGalleryExportResponse>), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ApiResult), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResult), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Create(Guid eventId, CancellationToken cancellationToken)
    {
        var result = await createGalleryExportHandler.HandleAsync(eventId, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("{exportId:guid}")]
    [ProducesResponseType(typeof(ApiResult<GetGalleryExportResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResult), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Get(
        Guid eventId,
        Guid exportId,
        CancellationToken cancellationToken)
    {
        var result = await getGalleryExportHandler.HandleAsync(eventId, exportId, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }
}
