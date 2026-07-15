using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Ortakare.Api.Common;
using Ortakare.Api.Features.Photos.DeleteGuestPhoto;
using Ortakare.Api.Features.Photos.UploadPhoto;
using Ortakare.Api.Infrastructure.RateLimiting;

namespace Ortakare.Api.Features.Photos;

[ApiController]
[AllowAnonymous]
[Route("api/public/events/{galleryToken}/photos")]
public sealed class PublicPhotosController(
    UploadPhotoHandler uploadPhotoHandler,
    DeleteGuestPhotoHandler deleteGuestPhotoHandler) : ControllerBase
{
    [HttpPost]
    [EnableRateLimiting(RateLimitingPolicies.Upload)]
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
        var result = await uploadPhotoHandler.HandleAsync(galleryToken, participantToken, clientUploadId, request, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpDelete("{photoId:guid}")]
    [EnableRateLimiting(RateLimitingPolicies.Public)]
    [ProducesResponseType(typeof(ApiResult<DeleteGuestPhotoResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResult), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResult), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        string galleryToken,
        Guid photoId,
        [FromHeader(Name = "X-Participant-Token")] string participantToken,
        CancellationToken cancellationToken)
    {
        var result = await deleteGuestPhotoHandler.HandleAsync(galleryToken, participantToken, photoId, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }
}
