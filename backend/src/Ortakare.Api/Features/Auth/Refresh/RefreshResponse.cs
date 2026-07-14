namespace Ortakare.Api.Features.Auth.Refresh;

public sealed record RefreshResponse(
    string AccessToken,
    DateTime AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTime RefreshTokenExpiresAtUtc);