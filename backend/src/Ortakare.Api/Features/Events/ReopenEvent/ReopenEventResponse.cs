namespace Ortakare.Api.Features.Events.ReopenEvent;

public sealed record ReopenEventResponse(
    Guid Id,
    bool UploadsEnabled,
    DateTime UpdatedAtUtc);
