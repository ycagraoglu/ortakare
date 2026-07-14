namespace Ortakare.Api.Features.Events.CloseEvent;

public sealed record CloseEventResponse(
    Guid Id,
    bool UploadsEnabled,
    DateTime UpdatedAtUtc);
