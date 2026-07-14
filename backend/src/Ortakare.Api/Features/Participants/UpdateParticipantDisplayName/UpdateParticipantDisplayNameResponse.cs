namespace Ortakare.Api.Features.Participants.UpdateParticipantDisplayName;

public sealed record UpdateParticipantDisplayNameResponse(
    Guid ParticipantId,
    string DisplayName);
