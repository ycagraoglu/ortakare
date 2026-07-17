using System.Text.Json;
using Ortakare.Api.Infrastructure.Realtime;

namespace Ortakare.Api.Tests.Infrastructure.Realtime;

public sealed class RealtimeEventFactoryTests
{
    [Fact]
    public void Create_Maps_Owner_Notification_Message()
    {
        var factory = new RealtimeEventFactory();
        var notificationId = Guid.NewGuid();
        var ownerUserId = Guid.NewGuid();
        var occurredAtUtc = DateTime.UtcNow;
        var payloadJson = JsonSerializer.Serialize(new
        {
            NotificationId = notificationId,
            OwnerUserId = ownerUserId,
            OccurredAtUtc = occurredAtUtc
        });

        var envelope = factory.Create("OwnerNotificationCreated", payloadJson);

        Assert.NotNull(envelope);
        Assert.Equal(ownerUserId, envelope.OwnerUserId);
        Assert.Equal("NotificationCreated", envelope.Event.Type);
        Assert.Equal(notificationId.ToString(), envelope.Event.EventId);
        Assert.Equal(payloadJson, envelope.Event.PayloadJson);
        Assert.Equal(occurredAtUtc, envelope.Event.OccurredAtUtc);
    }

    [Fact]
    public void Create_Returns_Null_For_Unsupported_Message_Type()
    {
        var factory = new RealtimeEventFactory();

        var envelope = factory.Create("UnsupportedMessage", "{}");

        Assert.Null(envelope);
    }

    [Theory]
    [InlineData("null")]
    [InlineData("{}")]
    public void Create_Throws_For_Invalid_Notification_Payload(string payloadJson)
    {
        var factory = new RealtimeEventFactory();

        Assert.Throws<InvalidOperationException>(() =>
            factory.Create("OwnerNotificationCreated", payloadJson));
    }
}
