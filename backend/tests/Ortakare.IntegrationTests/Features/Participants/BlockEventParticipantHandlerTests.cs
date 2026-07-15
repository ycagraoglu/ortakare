using Microsoft.EntityFrameworkCore;
using Ortakare.Api.Features.Events;
using Ortakare.Api.Features.Participants;
using Ortakare.Api.Features.Participants.BlockEventParticipant;
using Ortakare.Api.Infrastructure.Authentication;
using Ortakare.Api.Infrastructure.Persistence;

namespace Ortakare.IntegrationTests.Features.Participants;

public sealed class BlockEventParticipantHandlerTests
{
    [Fact]
    public async Task HandleAsync_BlocksParticipantOwnedByEventOwner()
    {
        var ownerUserId = Guid.CreateVersion7();
        var eventId = Guid.CreateVersion7();
        var participantId = Guid.CreateVersion7();
        var now = new DateTimeOffset(2026, 7, 15, 18, 0, 0, TimeSpan.Zero);
        await using var dbContext = CreateDbContext();
        dbContext.Events.Add(CreateEvent(eventId, ownerUserId));
        dbContext.EventGuestParticipants.Add(CreateParticipant(participantId, eventId));
        await dbContext.SaveChangesAsync();

        var handler = new BlockEventParticipantHandler(
            dbContext,
            new TestCurrentUser(ownerUserId),
            new TestTimeProvider(now));

        var result = await handler.HandleAsync(eventId, participantId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Data!.IsBlocked);
        Assert.Equal(now.UtcDateTime, result.Data.BlockedAtUtc);

        var participant = await dbContext.EventGuestParticipants.SingleAsync(x => x.Id == participantId);
        Assert.True(participant.IsBlocked);
        Assert.Equal(now.UtcDateTime, participant.BlockedAtUtc);
    }

    [Fact]
    public async Task HandleAsync_ReturnsNotFoundForAnotherOwnersEvent()
    {
        var eventId = Guid.CreateVersion7();
        var participantId = Guid.CreateVersion7();
        await using var dbContext = CreateDbContext();
        dbContext.Events.Add(CreateEvent(eventId, Guid.CreateVersion7()));
        dbContext.EventGuestParticipants.Add(CreateParticipant(participantId, eventId));
        await dbContext.SaveChangesAsync();

        var handler = new BlockEventParticipantHandler(
            dbContext,
            new TestCurrentUser(Guid.CreateVersion7()),
            new TestTimeProvider(DateTimeOffset.UtcNow));

        var result = await handler.HandleAsync(eventId, participantId, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.StatusCode);
        Assert.False((await dbContext.EventGuestParticipants.SingleAsync()).IsBlocked);
    }

    [Fact]
    public async Task HandleAsync_IsIdempotentWhenParticipantIsAlreadyBlocked()
    {
        var ownerUserId = Guid.CreateVersion7();
        var eventId = Guid.CreateVersion7();
        var participantId = Guid.CreateVersion7();
        var existingBlockedAt = new DateTime(2026, 7, 14, 9, 0, 0, DateTimeKind.Utc);
        await using var dbContext = CreateDbContext();
        dbContext.Events.Add(CreateEvent(eventId, ownerUserId));
        var participant = CreateParticipant(participantId, eventId);
        participant.IsBlocked = true;
        participant.BlockedAtUtc = existingBlockedAt;
        dbContext.EventGuestParticipants.Add(participant);
        await dbContext.SaveChangesAsync();

        var handler = new BlockEventParticipantHandler(
            dbContext,
            new TestCurrentUser(ownerUserId),
            new TestTimeProvider(new DateTimeOffset(2026, 7, 15, 18, 0, 0, TimeSpan.Zero)));

        var result = await handler.HandleAsync(eventId, participantId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Data!.IsBlocked);
        Assert.Equal(existingBlockedAt, result.Data.BlockedAtUtc);
    }

    private static OrtakareDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<OrtakareDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new OrtakareDbContext(options);
    }

    private static Event CreateEvent(Guid eventId, Guid ownerUserId) => new()
    {
        Id = eventId,
        OwnerUserId = ownerUserId,
        Title = "Test Event",
        EventDateUtc = new DateTime(2026, 8, 1, 18, 0, 0, DateTimeKind.Utc),
        GalleryToken = Guid.NewGuid().ToString("N"),
        UploadsEnabled = true,
        CreatedAtUtc = new DateTime(2026, 7, 1, 10, 0, 0, DateTimeKind.Utc)
    };

    private static EventGuestParticipant CreateParticipant(Guid participantId, Guid eventId) => new()
    {
        Id = participantId,
        EventId = eventId,
        DisplayName = "Guest",
        TokenHash = Guid.NewGuid().ToString("N"),
        CreatedAtUtc = new DateTime(2026, 7, 15, 10, 0, 0, DateTimeKind.Utc)
    };

    private sealed class TestCurrentUser(Guid userId) : ICurrentUser
    {
        public Guid UserId { get; } = userId;
    }

    private sealed class TestTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}