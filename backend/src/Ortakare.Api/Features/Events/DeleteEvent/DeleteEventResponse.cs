namespace Ortakare.Api.Features.Events.DeleteEvent;

public sealed record DeleteEventResponse(
    Guid Id,
    int DeletedPhotoCount,
    int DeletedParticipantCount,
    int DeletedExportCount);
