using Microsoft.AspNetCore.Mvc;
using Ortakare.Api.Common;
using Ortakare.Api.Features.Auth.Register;

namespace Ortakare.Api.Features.Auth;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(RegisterHandler registerHandler) : ControllerBase
{
    [HttpPost("register")]
    [ProducesResponseType(typeof(ApiResult<RegisterResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResult), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var result = await registerHandler.HandleAsync(request, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }
}
