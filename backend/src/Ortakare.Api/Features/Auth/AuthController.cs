using Microsoft.AspNetCore.Mvc;
using Ortakare.Api.Common;
using Ortakare.Api.Features.Auth.Login;
using Ortakare.Api.Features.Auth.Refresh;
using Ortakare.Api.Features.Auth.Register;

namespace Ortakare.Api.Features.Auth;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    RegisterHandler registerHandler,
    LoginHandler loginHandler,
    RefreshHandler refreshHandler) : ControllerBase
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

    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResult<LoginResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResult), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await loginHandler.HandleAsync(request, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("refresh")]
    [ProducesResponseType(typeof(ApiResult<RefreshResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResult), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh(
        [FromBody] RefreshRequest request,
        CancellationToken cancellationToken)
    {
        var result = await refreshHandler.HandleAsync(request, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }
}