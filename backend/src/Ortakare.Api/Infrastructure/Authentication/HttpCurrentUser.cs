using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Ortakare.Api.Infrastructure.Authentication;

public sealed class HttpCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public Guid UserId
    {
        get
        {
            var principal = httpContextAccessor.HttpContext?.User;
            var value = principal?.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? principal?.FindFirstValue(JwtRegisteredClaimNames.Sub);

            if (!Guid.TryParse(value, out var userId))
            {
                throw new InvalidOperationException("Authenticated user identifier is missing or invalid.");
            }

            return userId;
        }
    }
}
