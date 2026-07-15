namespace Ortakare.Api.Features.Participants.UnblockEventParticipant;

public sealed record UnblockEventParticipantResponse(
    Guid ParticipantId,
    bool IsBlocked,
    DateTime? BlockedAtUtc);
