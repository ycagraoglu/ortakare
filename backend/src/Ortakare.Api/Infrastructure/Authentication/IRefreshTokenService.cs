using Ortakare.Api.Features.Auth.RefreshTokens;

namespace Ortakare.Api.Infrastructure.Authentication;

public interface IRefreshTokenService
{
    RefreshTokenValue Create(Guid userId);
    string Hash(string token);
}

public sealed record RefreshTokenValue(
    string Value,
    RefreshToken Entity);