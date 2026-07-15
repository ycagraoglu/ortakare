using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Ortakare.Api.Features.Dashboard.GetOwnerStorageQuota;
using Ortakare.Api.Features.Events;
using Ortakare.Api.Features.Participants;
using Ortakare.Api.Features.Photos;
using Ortakare.Api.Infrastructure.Authentication;
using Ortakare.Api.Infrastructure.Persistence;

namespace Ortakare.IntegrationTests.Features.Dashboard;

public sealed class GetOwnerStorageQuotaHandlerTests
{
    [Theory]
    [InlineData(700, OwnerStorageQuotaStatus.Healthy)]
    [InlineData(800, OwnerStorageQuotaStatus.Warning)]
    [InlineData(950, OwnerStorageQuotaStatus.Critical)]
    [InlineData(1001, OwnerStorageQuotaStatus.Exceeded)]
    public async Task HandleAsync_ReturnsExpectedQuotaStatus(
        long usedBytes,
        OwnerStorageQuotaStatus expectedStatus)
    {
        var ownerUserId = Guid.CreateVersion7();
        var eventId = Guid.CreateVersion7();
        var participantId = Guid.CreateVersion7();
        await using var dbContext = CreateDbContext();

        dbContext.Events.Add(CreateEvent(eventId, ownerUserId));
        dbContext.EventGuestParticipants.Add(CreateParticipant(participantId, eventId));
        dbContext.EventGuestPhotos.Add(CreatePhoto(eventId, participantId, usedBytes));
        await dbContext.SaveChangesAsync();

        var handler = new GetOwnerStorageQuotaHandler(
            dbContext,
            new TestCurrentUser(ownerUserId),
            CreateConfiguration());

        var result = await handler.HandleAsync(CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(expectedStatus, result.Data.Status);
        Assert.Equal(usedBytes, result.Data.UsedBytes);
        Assert.Equal(Math.Max(0, 1_000 - usedBytes), result.Data.RemainingBytes);
        Assert.Equal(Math.Max(0, usedBytes - 1_000), result.Data.OverQuotaBytes);
    }

    [Fact]
    public async Task HandleAsync_ExcludesAnotherOwnersStorage()
    {
        var ownerUserId = Guid.CreateVersion7();
        var otherOwnerUserId = Guid.CreateVersion7();
        var ownerEventId = Guid.CreateVersion7();
        var otherEventId = Guid.CreateVersion7();
        var ownerParticipantId = Guid.CreateVersion7();
        var otherParticipantId = Guid.CreateVersion7();
        await using var dbContext = CreateDbContext();

        dbContext.Events.AddRange(
            CreateEvent(ownerEventId, ownerUserId),
            CreateEvent(otherEventId, otherOwnerUserId));
        dbContext.EventGuestParticipants.AddRange(
            CreateParticipant(ownerParticipantId, ownerEventId),
            CreateParticipant(otherParticipantId, otherEventId));
        dbContext.EventGuestPhotos.AddRange(
            CreatePhoto(ownerEventId, ownerParticipantId, 400),
            CreatePhoto(otherEventId, otherParticipantId, 900));
        await dbContext.SaveChangesAsync();

        var handler = new GetOwnerStorageQuotaHandler(
            dbContext,
            new TestCurrentUser(ownerUserId),
            CreateConfiguration());

        var result = await handler.HandleAsync(CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(400, result.Data!.UsedBytes);
        Assert.Equal(OwnerStorageQuotaStatus.Healthy, result.Data.Status);
    }

    private static IConfiguration CreateConfiguration() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["OwnerStorageQuota:QuotaBytes"] = "1000",
                ["OwnerStorageQuota:WarningThresholdPercent"] = "80",
                ["OwnerStorageQuota:CriticalThresholdPercent"] = "95"
            })
            .Build();

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
        Title = "Quota Event",
        EventDateUtc = DateTime.UtcNow,
        GalleryToken = Guid.NewGuid().ToString("N"),
        UploadsEnabled = true,
        CreatedAtUtc = DateTime.UtcNow
    };

    private static EventGuestParticipant CreateParticipant(Guid participantId, Guid eventId) => new()
    {
        Id = participantId,
        EventId = eventId,
        DisplayName = "Guest",
        TokenHash = Guid.NewGuid().ToString("N"),
        CreatedAtUtc = DateTime.UtcNow
    };

    private static EventGuestPhoto CreatePhoto(Guid eventId, Guid participantId, long sizeBytes) => new()
    {
        Id = Guid.CreateVersion7(),
        EventId = eventId,
        ParticipantId = participantId,
        ClientUploadId = Guid.CreateVersion7(),
        StorageKey = Guid.NewGuid().ToString("N"),
        OriginalFileName = "photo.jpg",
        ContentType = "image/jpeg",
        FileSizeBytes = sizeBytes,
        CreatedAtUtc = DateTime.UtcNow
    };

    private sealed class TestCurrentUser(Guid userId) : ICurrentUser
    {
        public Guid UserId { get; } = userId;
    }
}
