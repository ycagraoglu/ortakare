using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Ortakare.Api.Common;
using Ortakare.Api.Features.Auth.Login;
using Ortakare.Api.Features.Auth.Logout;
using Ortakare.Api.Features.Auth.Refresh;
using Ortakare.Api.Features.Auth.Register;
using Ortakare.Api.Infrastructure.RateLimiting;

namespace Ortakare.Api.Features.Auth;

[ApiController]
[EnableRateLimiting(RateLimitingPolicies.Auth)]
[Route("api/auth")]
public sealed class AuthController(
    RegisterHandler registerHandler,
    LoginHandler loginHandler,
    RefreshHandler refreshHandler,
    LogoutHandler logoutHandler) : ControllerBase
{
    [HttpPost("register")]
    [ProducesResponseType(typeof(ApiResult<RegisterResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResult), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var result = await registerHandler.HandleAsync(request, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResult<LoginResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResult), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await loginHandler.HandleAsync(request, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("refresh")]
    [ProducesResponseType(typeof(ApiResult<RefreshResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResult), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request, CancellationToken cancellationToken)
    {
        var result = await refreshHandler.HandleAsync(request, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("logout")]
    [ProducesResponseType(typeof(ApiResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResult), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest request, CancellationToken cancellationToken)
    {
        var result = await logoutHandler.HandleAsync(request, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }
}
