namespace Ortakare.Api.Features.Events.GetEvent;

public sealed record GetEventResponse(
    Guid Id,
    string Title,
    DateTime EventDateUtc,
    string GalleryToken,
    bool UploadsEnabled,
    DateTime CreatedAtUtc);
