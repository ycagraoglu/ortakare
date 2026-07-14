namespace Ortakare.Api.Features.Auth.Login;

public sealed record LoginResponse(
    string AccessToken,
    DateTime ExpiresAtUtc,
    Guid UserId,
    string DisplayName,
    string Email);
