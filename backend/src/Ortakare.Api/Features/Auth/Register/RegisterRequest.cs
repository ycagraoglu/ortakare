namespace Ortakare.Api.Features.Auth.Register;

public sealed record RegisterRequest(
    string DisplayName,
    string Email,
    string Password);
