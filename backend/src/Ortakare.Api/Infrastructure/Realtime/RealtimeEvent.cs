namespace Ortakare.Api.Infrastructure.Realtime;

public sealed record RealtimeEvent(
    string Type,
    string PayloadJson,
    string? EventId = null,
    DateTime? OccurredAtUtc = null);

public interface IRealtimePublisher
{
    ValueTask PublishAsync(
        Guid ownerUserId,
        RealtimeEvent realtimeEvent,
        CancellationToken cancellationToken = default);
}
