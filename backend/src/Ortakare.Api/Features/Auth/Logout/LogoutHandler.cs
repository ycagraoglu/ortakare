using Microsoft.EntityFrameworkCore;
using Ortakare.Api.Common;
using Ortakare.Api.Infrastructure.Authentication;
using Ortakare.Api.Infrastructure.Persistence;

namespace Ortakare.Api.Features.Auth.Logout;

public sealed class LogoutHandler(
    OrtakareDbContext dbContext,
    IRefreshTokenService refreshTokenService,
    TimeProvider timeProvider)
{
    public async Task<ApiResult> HandleAsync(
        LogoutRequest request,
        CancellationToken cancellationToken)
    {
        var tokenHash = refreshTokenService.Hash(request.RefreshToken);
        var refreshToken = await dbContext.RefreshTokens
            .SingleOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);

        if (refreshToken is not null && refreshToken.RevokedAtUtc is null)
        {
            refreshToken.RevokedAtUtc = timeProvider.GetUtcNow().UtcDateTime;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return ApiResult.Success("Oturum kapatıldı.");
    }
}
