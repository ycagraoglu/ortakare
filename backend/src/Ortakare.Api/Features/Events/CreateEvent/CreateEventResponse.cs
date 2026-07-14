namespace Ortakare.Api.Features.Events.CreateEvent;

public sealed record CreateEventResponse(
    Guid Id,
    string Title,
    DateTime EventDateUtc,
    string GalleryToken,
    bool UploadsEnabled,
    DateTime CreatedAtUtc);
