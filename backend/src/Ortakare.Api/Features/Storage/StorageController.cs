using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ortakare.Api.Common;
using Ortakare.Api.Features.Storage.ValidateUpload;

namespace Ortakare.Api.Features.Storage;

[ApiController]
[Authorize]
[Route("api/storage")]
public sealed class StorageController(
    ValidateUploadHandler validateUploadHandler) : ControllerBase
{
    [HttpPost("validate-upload")]
    [ProducesResponseType(typeof(ApiResult<ValidateUploadResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResult), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ValidateUpload(
        [FromBody] ValidateUploadRequest request,
        CancellationToken cancellationToken)
    {
        var result = await validateUploadHandler.HandleAsync(request, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }
}