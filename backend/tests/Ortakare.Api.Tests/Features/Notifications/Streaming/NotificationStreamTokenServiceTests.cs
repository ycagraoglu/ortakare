using Microsoft.Extensions.Options;
using Ortakare.Api.Features.Notifications.Streaming;

namespace Ortakare.Api.Tests.Features.Notifications.Streaming;

public sealed class NotificationStreamTokenServiceTests
{
    [Fact]
    public void Issue_And_Consume_Returns_Owner_And_Expiry()
    {
        var now = new DateTimeOffset(2026, 7, 17, 9, 0, 0, TimeSpan.Zero);
        var timeProvider = new TestTimeProvider(now);
        var service = CreateService(timeProvider, 60);
        var ownerUserId = Guid.NewGuid();

        var issued = service.Issue(ownerUserId);
        var consumed = service.Consume(issued.Token);

        Assert.NotNull(consumed);
        Assert.Equal(ownerUserId, consumed.OwnerUserId);
        Assert.Equal(now.AddSeconds(60).UtcDateTime, consumed.ExpiresAtUtc);
        Assert.Equal(consumed.ExpiresAtUtc, issued.ExpiresAtUtc);
    }

    [Fact]
    public void Consume_Can_Use_Token_Only_Once()
    {
        var service = CreateService(new TestTimeProvider(DateTimeOffset.UtcNow), 60);
        var issued = service.Issue(Guid.NewGuid());

        var firstConsume = service.Consume(issued.Token);
        var secondConsume = service.Consume(issued.Token);

        Assert.NotNull(firstConsume);
        Assert.Null(secondConsume);
    }

    [Fact]
    public void Consume_Returns_Null_When_Token_Is_Expired()
    {
        var now = new DateTimeOffset(2026, 7, 17, 9, 0, 0, TimeSpan.Zero);
        var timeProvider = new TestTimeProvider(now);
        var service = CreateService(timeProvider, 60);
        var issued = service.Issue(Guid.NewGuid());

        timeProvider.Advance(TimeSpan.FromSeconds(61));
        var consumed = service.Consume(issued.Token);

        Assert.Null(consumed);
    }

    [Fact]
    public void Consume_Returns_Null_For_Unknown_Or_Empty_Token()
    {
        var service = CreateService(new TestTimeProvider(DateTimeOffset.UtcNow), 60);

        Assert.Null(service.Consume(string.Empty));
        Assert.Null(service.Consume("unknown-token"));
    }

    [Fact]
    public void Issue_Creates_Different_Tokens_For_The_Same_Owner()
    {
        var service = CreateService(new TestTimeProvider(DateTimeOffset.UtcNow), 60);
        var ownerUserId = Guid.NewGuid();

        var first = service.Issue(ownerUserId);
        var second = service.Issue(ownerUserId);

        Assert.NotEqual(first.Token, second.Token);
        Assert.NotNull(service.Consume(first.Token));
        Assert.NotNull(service.Consume(second.Token));
    }

    private static NotificationStreamTokenService CreateService(
        TimeProvider timeProvider,
        int lifetimeSeconds)
    {
        return new NotificationStreamTokenService(
            Options.Create(new NotificationStreamOptions
            {
                TokenLifetimeSeconds = lifetimeSeconds
            }),
            timeProvider);
    }

    private sealed class TestTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        private DateTimeOffset _utcNow = utcNow;

        public override DateTimeOffset GetUtcNow() => _utcNow;

        public void Advance(TimeSpan duration)
        {
            _utcNow = _utcNow.Add(duration);
        }
    }
}
