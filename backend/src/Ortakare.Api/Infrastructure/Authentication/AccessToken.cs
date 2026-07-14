namespace Ortakare.Api.Infrastructure.Authentication;

public sealed record AccessToken(
    string Value,
    DateTime ExpiresAtUtc);
