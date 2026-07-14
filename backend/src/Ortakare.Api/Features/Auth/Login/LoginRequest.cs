namespace Ortakare.Api.Features.Auth.Login;

public sealed record LoginRequest(
    string Email,
    string Password);
