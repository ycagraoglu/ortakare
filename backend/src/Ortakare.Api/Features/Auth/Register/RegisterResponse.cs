namespace Ortakare.Api.Features.Auth.Register;

public sealed record RegisterResponse(
    Guid Id,
    string DisplayName,
    string Email,
    DateTime CreatedAtUtc);
