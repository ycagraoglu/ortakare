namespace Ortakare.Api.Features.Participants.BlockEventParticipant;

public sealed record BlockEventParticipantResponse(
    Guid ParticipantId,
    bool IsBlocked,
    DateTime BlockedAtUtc);