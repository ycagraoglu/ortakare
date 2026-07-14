using System.Security.Cryptography;
using System.Text;
using Ortakare.Api.Features.Auth.RefreshTokens;

namespace Ortakare.Api.Infrastructure.Authentication;

public sealed class RefreshTokenService(
    TimeProvider timeProvider,
    IConfiguration configuration) : IRefreshTokenService
{
    public RefreshTokenValue Create(Guid userId)
    {
        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var utcNow = timeProvider.GetUtcNow().UtcDateTime;
        var lifetimeDays = configuration.GetValue<int?>("Jwt:RefreshTokenLifetimeDays") ?? 30;

        var entity = new RefreshToken
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            TokenHash = Hash(rawToken),
            CreatedAtUtc = utcNow,
            ExpiresAtUtc = utcNow.AddDays(lifetimeDays)
        };

        return new RefreshTokenValue(rawToken, entity);
    }

    public string Hash(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }
}