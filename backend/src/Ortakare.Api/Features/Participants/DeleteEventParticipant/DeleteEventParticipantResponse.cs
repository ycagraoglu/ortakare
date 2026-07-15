namespace Ortakare.Api.Features.Participants.DeleteEventParticipant;

public sealed record DeleteEventParticipantResponse(
    Guid ParticipantId,
    int DeletedPhotoCount);
