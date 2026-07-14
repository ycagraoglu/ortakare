using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ortakare.Api.Common;
using Ortakare.Api.Features.Photos.UploadPhoto;

namespace Ortakare.Api.Features.Photos;

[ApiController]
[AllowAnonymous]
[Route("api/public/events/{galleryToken}/photos")]
public sealed class PublicPhotosController(UploadPhotoHandler uploadPhotoHandler) : ControllerBase
{
    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResult<UploadPhotoResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResult<UploadPhotoResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResult), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResult), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResult), StatusCodes.Status415UnsupportedMediaType)]
    public async Task<IActionResult> Upload(
        string galleryToken,
        [FromHeader(Name = "X-Participant-Token")] string participantToken,
        [FromHeader(Name = "X-Client-Upload-Id")] Guid clientUploadId,
        [FromForm] UploadPhotoRequest request,
        CancellationToken cancellationToken)
    {
        var result = await uploadPhotoHandler.HandleAsync(
            galleryToken,
            participantToken,
            clientUploadId,
            request,
            cancellationToken);

        return StatusCode(result.StatusCode, result);
    }
}