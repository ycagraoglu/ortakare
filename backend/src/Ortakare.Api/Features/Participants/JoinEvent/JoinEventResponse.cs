namespace Ortakare.Api.Features.Participants.JoinEvent;

public sealed record JoinEventResponse(
    string ParticipantToken,
    string DisplayName,
    DateTime CreatedAtUtc);
