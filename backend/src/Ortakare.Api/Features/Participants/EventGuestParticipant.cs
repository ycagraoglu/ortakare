namespace Ortakare.Api.Features.Participants;

public sealed class EventGuestParticipant
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string TokenHash { get; set; } = string.Empty;
    public bool IsBlocked { get; set; }
    public DateTime? BlockedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}