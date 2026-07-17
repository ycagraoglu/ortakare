using Ortakare.Api.Features.Notifications.Streaming;

namespace Ortakare.Api.Tests.Features.Notifications.Streaming;

public sealed class NotificationSseConnectionManagerTests
{
    [Fact]
    public async Task PublishAsync_Delivers_Event_To_All_Owner_Connections()
    {
        var manager = new NotificationSseConnectionManager();
        var ownerUserId = Guid.NewGuid();
        await using var first = manager.Subscribe(ownerUserId);
        await using var second = manager.Subscribe(ownerUserId);
        var notificationEvent = new NotificationSseEvent("notification-created", "{\"id\":\"1\"}");

        await manager.PublishAsync(ownerUserId, notificationEvent);

        Assert.True(first.Reader.TryRead(out var firstEvent));
        Assert.True(second.Reader.TryRead(out var secondEvent));
        Assert.Equal(notificationEvent, firstEvent);
        Assert.Equal(notificationEvent, secondEvent);
        Assert.Equal(2, manager.GetConnectionCount(ownerUserId));
    }

    [Fact]
    public async Task PublishAsync_Does_Not_Deliver_To_Different_Owner()
    {
        var manager = new NotificationSseConnectionManager();
        var ownerUserId = Guid.NewGuid();
        var differentOwnerUserId = Guid.NewGuid();
        await using var subscription = manager.Subscribe(ownerUserId);

        await manager.PublishAsync(
            differentOwnerUserId,
            new NotificationSseEvent("notification-created", "{}"));

        Assert.False(subscription.Reader.TryRead(out _));
    }

    [Fact]
    public async Task DisposeAsync_Removes_Only_The_Disposed_Connection()
    {
        var manager = new NotificationSseConnectionManager();
        var ownerUserId = Guid.NewGuid();
        var first = manager.Subscribe(ownerUserId);
        await using var second = manager.Subscribe(ownerUserId);

        await first.DisposeAsync();

        Assert.Equal(1, manager.GetConnectionCount(ownerUserId));

        var notificationEvent = new NotificationSseEvent("heartbeat", "{}");
        await manager.PublishAsync(ownerUserId, notificationEvent);

        Assert.True(second.Reader.TryRead(out var deliveredEvent));
        Assert.Equal(notificationEvent, deliveredEvent);
        Assert.False(first.Reader.TryRead(out _));
    }

    [Fact]
    public async Task DisposeAsync_Is_Idempotent()
    {
        var manager = new NotificationSseConnectionManager();
        var ownerUserId = Guid.NewGuid();
        var subscription = manager.Subscribe(ownerUserId);

        await subscription.DisposeAsync();
        await subscription.DisposeAsync();

        Assert.Equal(0, manager.GetConnectionCount(ownerUserId));
    }
}
