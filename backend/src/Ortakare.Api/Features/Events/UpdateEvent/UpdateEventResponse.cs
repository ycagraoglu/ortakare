namespace Ortakare.Api.Features.Events.UpdateEvent;

public sealed record UpdateEventResponse(
    Guid Id,
    string Title,
    DateTime EventDateUtc,
    bool UploadsEnabled,
    DateTime UpdatedAtUtc);