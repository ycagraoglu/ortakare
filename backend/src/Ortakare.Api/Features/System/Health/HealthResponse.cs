namespace Ortakare.Api.Features.System.Health;

public sealed record HealthResponse(
    string Service,
    string Status,
    DateTime TimestampUtc);
