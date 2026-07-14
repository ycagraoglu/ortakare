using Microsoft.EntityFrameworkCore;
using Ortakare.Api.Common;
using Ortakare.Api.Features.Auth.RefreshTokens;
using Ortakare.Api.Infrastructure.Authentication;
using Ortakare.Api.Infrastructure.Persistence;

namespace Ortakare.Api.Features.Auth.Refresh;

public sealed class RefreshHandler(
    OrtakareDbContext dbContext,
    IAccessTokenService accessTokenService,
    IRefreshTokenService refreshTokenService,
    TimeProvider timeProvider)
{
    public async Task<ApiResult<RefreshResponse>> HandleAsync(
        RefreshRequest request,
        CancellationToken cancellationToken)
    {
        var tokenHash = refreshTokenService.Hash(request.RefreshToken);
        var utcNow = timeProvider.GetUtcNow().UtcDateTime;

        var currentToken = await dbContext.RefreshTokens
            .SingleOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);

        if (currentToken is null || !currentToken.IsActive(utcNow))
        {
            return ApiResult<RefreshResponse>.Failure(
                "Refresh token geçersiz veya süresi dolmuş.",
                StatusCodes.Status401Unauthorized);
        }

        var user = await dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == currentToken.UserId, cancellationToken);

        if (user is null)
        {
            return ApiResult<RefreshResponse>.Failure(
                "Refresh token geçersiz veya süresi dolmuş.",
                StatusCodes.Status401Unauthorized);
        }

        var newRefreshToken = refreshTokenService.Create(user.Id);
        currentToken.UsedAtUtc = utcNow;
        currentToken.ReplacedByTokenId = newRefreshToken.Entity.Id;

        dbContext.RefreshTokens.Add(newRefreshToken.Entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        var accessToken = accessTokenService.Create(user);

        return ApiResult<RefreshResponse>.Success(
            new RefreshResponse(
                accessToken.Value,
                accessToken.ExpiresAtUtc,
                newRefreshToken.Value,
                newRefreshToken.Entity.ExpiresAtUtc));
    }
}