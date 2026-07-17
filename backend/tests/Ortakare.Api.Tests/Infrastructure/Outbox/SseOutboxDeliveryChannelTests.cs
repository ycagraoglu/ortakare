using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Ortakare.Api.Features.Notifications.Streaming;
using Ortakare.Api.Infrastructure.Outbox;

namespace Ortakare.Api.Tests.Infrastructure.Outbox;

public sealed class SseOutboxDeliveryChannelTests
{
    [Fact]
    public async Task DeliverAsync_Publishes_Owner_Notification_As_Sse_Event()
    {
        var publisher = new RecordingRealtimePublisher();
        var channel = CreateChannel(publisher);
        var notificationId = Guid.NewGuid();
        var ownerUserId = Guid.NewGuid();
        var payloadJson = JsonSerializer.Serialize(new
        {
            NotificationId = notificationId,
            OwnerUserId = ownerUserId,
            EventId = Guid.NewGuid(),
            NotificationType = "PhotoUploaded",
            Severity = "Info",
            Title = "Yeni fotoğraf",
            Message = "Etkinliğe yeni bir fotoğraf yüklendi.",
            OccurredAtUtc = DateTime.UtcNow
        });

        await channel.DeliverAsync(
            "OwnerNotificationCreated",
            payloadJson,
            CancellationToken.None);

        var published = Assert.Single(publisher.PublishedEvents);
        Assert.Equal(ownerUserId, published.OwnerUserId);
        Assert.Equal("notification-created", published.Event.EventName);
        Assert.Equal(notificationId.ToString(), published.Event.EventId);
        Assert.Equal(payloadJson, published.Event.Data);
    }

    [Fact]
    public async Task DeliverAsync_Ignores_Unrelated_Message_Type()
    {
        var publisher = new RecordingRealtimePublisher();
        var channel = CreateChannel(publisher);

        await channel.DeliverAsync(
            "GalleryExportRequested",
            "{}",
            CancellationToken.None);

        Assert.Empty(publisher.PublishedEvents);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("null")]
    public async Task DeliverAsync_Throws_When_Notification_Payload_Is_Invalid(string payloadJson)
    {
        var channel = CreateChannel(new RecordingRealtimePublisher());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            channel.DeliverAsync(
                "OwnerNotificationCreated",
                payloadJson,
                CancellationToken.None));
    }

    private static SseOutboxDeliveryChannel CreateChannel(INotificationRealtimePublisher publisher)
    {
        return new SseOutboxDeliveryChannel(
            publisher,
            NullLogger<SseOutboxDeliveryChannel>.Instance);
    }

    private sealed class RecordingRealtimePublisher : INotificationRealtimePublisher
    {
        public List<PublishedEvent> PublishedEvents { get; } = [];

        public ValueTask PublishAsync(
            Guid ownerUserId,
            NotificationSseEvent notificationEvent,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            PublishedEvents.Add(new PublishedEvent(ownerUserId, notificationEvent));
            return ValueTask.CompletedTask;
        }
    }

    private sealed record PublishedEvent(
        Guid OwnerUserId,
        NotificationSseEvent Event);
}
