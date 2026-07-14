using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Ortakare.Api.Common;
using Ortakare.Api.Features.Users;
using Ortakare.Api.Infrastructure.Authentication;
using Ortakare.Api.Infrastructure.Persistence;

namespace Ortakare.Api.Features.Auth.Login;

public sealed class LoginHandler(
    OrtakareDbContext dbContext,
    IPasswordHasher<User> passwordHasher,
    IAccessTokenService accessTokenService,
    IRefreshTokenService refreshTokenService)
{
    public async Task<ApiResult<LoginResponse>> HandleAsync(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToUpperInvariant();

        var user = await dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.NormalizedEmail == normalizedEmail,
                cancellationToken);

        if (user is null)
        {
            return ApiResult<LoginResponse>.Failure(
                "E-posta adresi veya şifre hatalı.",
                StatusCodes.Status401Unauthorized);
        }

        var verificationResult = passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            request.Password);

        if (verificationResult == PasswordVerificationResult.Failed)
        {
            return ApiResult<LoginResponse>.Failure(
                "E-posta adresi veya şifre hatalı.",
                StatusCodes.Status401Unauthorized);
        }

        var accessToken = accessTokenService.Create(user);
        var refreshToken = refreshTokenService.Create(user.Id);

        dbContext.RefreshTokens.Add(refreshToken.Entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ApiResult<LoginResponse>.Success(
            new LoginResponse(
                accessToken.Value,
                accessToken.ExpiresAtUtc,
                refreshToken.Value,
                refreshToken.Entity.ExpiresAtUtc,
                user.Id,
                user.DisplayName,
                user.Email));
    }
}