using System.Collections.Concurrent;
using System.Threading.Channels;

namespace Ortakare.Api.Features.Notifications.Streaming;

public sealed record NotificationSseEvent(
    string EventName,
    string Data,
    string? EventId = null);

public interface INotificationRealtimePublisher
{
    ValueTask PublishAsync(
        Guid ownerUserId,
        NotificationSseEvent notificationEvent,
        CancellationToken cancellationToken = default);
}

public interface INotificationSseConnectionManager : INotificationRealtimePublisher
{
    NotificationSseSubscription Subscribe(Guid ownerUserId);
    int GetConnectionCount(Guid ownerUserId);
}

public sealed class NotificationSseConnectionManager : INotificationSseConnectionManager
{
    private const int ChannelCapacity = 100;
    private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<Guid, Channel<NotificationSseEvent>>> _connections = new();

    public NotificationSseSubscription Subscribe(Guid ownerUserId)
    {
        var connectionId = Guid.NewGuid();
        var channel = Channel.CreateBounded<NotificationSseEvent>(new BoundedChannelOptions(ChannelCapacity)
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.DropOldest,
            AllowSynchronousContinuations = false
        });

        var ownerConnections = _connections.GetOrAdd(
            ownerUserId,
            static _ => new ConcurrentDictionary<Guid, Channel<NotificationSseEvent>>());

        ownerConnections[connectionId] = channel;

        return new NotificationSseSubscription(
            ownerUserId,
            connectionId,
            channel.Reader,
            RemoveConnection);
    }

    public ValueTask PublishAsync(
        Guid ownerUserId,
        NotificationSseEvent notificationEvent,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_connections.TryGetValue(ownerUserId, out var ownerConnections))
        {
            return ValueTask.CompletedTask;
        }

        foreach (var connection in ownerConnections.Values)
        {
            connection.Writer.TryWrite(notificationEvent);
        }

        return ValueTask.CompletedTask;
    }

    public int GetConnectionCount(Guid ownerUserId)
    {
        return _connections.TryGetValue(ownerUserId, out var ownerConnections)
            ? ownerConnections.Count
            : 0;
    }

    private void RemoveConnection(Guid ownerUserId, Guid connectionId)
    {
        if (!_connections.TryGetValue(ownerUserId, out var ownerConnections))
        {
            return;
        }

        if (ownerConnections.TryRemove(connectionId, out var channel))
        {
            channel.Writer.TryComplete();
        }

        if (ownerConnections.IsEmpty)
        {
            _connections.TryRemove(
                new KeyValuePair<Guid, ConcurrentDictionary<Guid, Channel<NotificationSseEvent>>>(
                    ownerUserId,
                    ownerConnections));
        }
    }
}

public sealed class NotificationSseSubscription : IAsyncDisposable
{
    private readonly Action<Guid, Guid> _removeConnection;
    private int _disposed;

    internal NotificationSseSubscription(
        Guid ownerUserId,
        Guid connectionId,
        ChannelReader<NotificationSseEvent> reader,
        Action<Guid, Guid> removeConnection)
    {
        OwnerUserId = ownerUserId;
        ConnectionId = connectionId;
        Reader = reader;
        _removeConnection = removeConnection;
    }

    public Guid OwnerUserId { get; }
    public Guid ConnectionId { get; }
    public ChannelReader<NotificationSseEvent> Reader { get; }

    public ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 0)
        {
            _removeConnection(OwnerUserId, ConnectionId);
        }

        return ValueTask.CompletedTask;
    }
}
