using Microsoft.EntityFrameworkCore;
using Ortakare.Api.Features.Events;
using Ortakare.Api.Features.Participants;
using Ortakare.Api.Features.Participants.UnblockEventParticipant;
using Ortakare.Api.Infrastructure.Authentication;
using Ortakare.Api.Infrastructure.Persistence;

namespace Ortakare.IntegrationTests.Features.Participants;

public sealed class UnblockEventParticipantHandlerTests
{
    [Fact]
    public async Task HandleAsync_UnblocksOwnedEventParticipant()
    {
        var ownerUserId = Guid.CreateVersion7();
        var eventId = Guid.CreateVersion7();
        var participantId = Guid.CreateVersion7();
        await using var dbContext = CreateDbContext();
        dbContext.Events.Add(CreateEvent(eventId, ownerUserId));
        dbContext.EventGuestParticipants.Add(CreateParticipant(participantId, eventId, isBlocked: true));
        await dbContext.SaveChangesAsync();

        var handler = new UnblockEventParticipantHandler(
            dbContext,
            new TestCurrentUser(ownerUserId));

        var result = await handler.HandleAsync(eventId, participantId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Data!.IsBlocked);
        Assert.Null(result.Data.BlockedAtUtc);

        var participant = await dbContext.EventGuestParticipants.SingleAsync(x => x.Id == participantId);
        Assert.False(participant.IsBlocked);
        Assert.Null(participant.BlockedAtUtc);
    }

    [Fact]
    public async Task HandleAsync_ReturnsNotFoundForAnotherOwnersEvent()
    {
        var eventId = Guid.CreateVersion7();
        var participantId = Guid.CreateVersion7();
        await using var dbContext = CreateDbContext();
        dbContext.Events.Add(CreateEvent(eventId, Guid.CreateVersion7()));
        dbContext.EventGuestParticipants.Add(CreateParticipant(participantId, eventId, isBlocked: true));
        await dbContext.SaveChangesAsync();

        var handler = new UnblockEventParticipantHandler(
            dbContext,
            new TestCurrentUser(Guid.CreateVersion7()));

        var result = await handler.HandleAsync(eventId, participantId, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.StatusCode);
        Assert.True((await dbContext.EventGuestParticipants.SingleAsync(x => x.Id == participantId)).IsBlocked);
    }

    [Fact]
    public async Task HandleAsync_IsIdempotentWhenParticipantIsAlreadyActive()
    {
        var ownerUserId = Guid.CreateVersion7();
        var eventId = Guid.CreateVersion7();
        var participantId = Guid.CreateVersion7();
        await using var dbContext = CreateDbContext();
        dbContext.Events.Add(CreateEvent(eventId, ownerUserId));
        dbContext.EventGuestParticipants.Add(CreateParticipant(participantId, eventId, isBlocked: false));
        await dbContext.SaveChangesAsync();

        var handler = new UnblockEventParticipantHandler(
            dbContext,
            new TestCurrentUser(ownerUserId));

        var result = await handler.HandleAsync(eventId, participantId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Data!.IsBlocked);
        Assert.Null(result.Data.BlockedAtUtc);
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

    private static EventGuestParticipant CreateParticipant(
        Guid participantId,
        Guid eventId,
        bool isBlocked) => new()
    {
        Id = participantId,
        EventId = eventId,
        DisplayName = "Test Guest",
        TokenHash = Guid.NewGuid().ToString("N").PadRight(64, '0'),
        IsBlocked = isBlocked,
        BlockedAtUtc = isBlocked
            ? new DateTime(2026, 7, 15, 18, 0, 0, DateTimeKind.Utc)
            : null,
        CreatedAtUtc = new DateTime(2026, 7, 15, 17, 0, 0, DateTimeKind.Utc)
    };

    private sealed class TestCurrentUser(Guid userId) : ICurrentUser
    {
        public Guid UserId { get; } = userId;
    }
}
