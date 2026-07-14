using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Ortakare.Api.Common;
using Ortakare.Api.Features.Events;
using Ortakare.Api.Features.Participants;
using Ortakare.Api.Features.Participants.UpdateParticipantDisplayName;
using Ortakare.Api.Features.Users;
using Ortakare.Api.Infrastructure.Persistence;

namespace Ortakare.IntegrationTests.Participants;

public sealed class UpdateParticipantDisplayNameEndpointTests : IClassFixture<OrtakareApiFactory>
{
    private readonly OrtakareApiFactory _factory;

    public UpdateParticipantDisplayNameEndpointTests(OrtakareApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task UpdateDisplayName_updates_authenticated_participant(
        CancellationToken cancellationToken)
    {
        var seeded = await SeedParticipantAsync(uploadsEnabled: true, cancellationToken);
        using var client = _factory.CreateClient();
        using var request = CreateRequest(
            seeded.GalleryToken,
            seeded.ParticipantToken,
            "  Yeni Rumuz  ");

        var response = await client.SendAsync(request, cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content
            .ReadFromJsonAsync<ApiResult<UpdateParticipantDisplayNameResponse>>(cancellationToken);

        Assert.NotNull(result?.Data);
        Assert.Equal(seeded.ParticipantId, result.Data.ParticipantId);
        Assert.Equal("Yeni Rumuz", result.Data.DisplayName);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrtakareDbContext>();
        var participant = await dbContext.EventGuestParticipants
            .AsNoTracking()
            .SingleAsync(x => x.Id == seeded.ParticipantId, cancellationToken);

        Assert.Equal("Yeni Rumuz", participant.DisplayName);
    }

    [Fact]
    public async Task UpdateDisplayName_allows_existing_participant_when_uploads_are_closed(
        CancellationToken cancellationToken)
    {
        var seeded = await SeedParticipantAsync(uploadsEnabled: false, cancellationToken);
        using var client = _factory.CreateClient();
        using var request = CreateRequest(
            seeded.GalleryToken,
            seeded.ParticipantToken,
            "Kapalı Albüm Misafiri");

        var response = await client.SendAsync(request, cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UpdateDisplayName_rejects_token_from_another_event(
        CancellationToken cancellationToken)
    {
        var first = await SeedParticipantAsync(uploadsEnabled: true, cancellationToken);
        var second = await SeedParticipantAsync(uploadsEnabled: true, cancellationToken);
        using var client = _factory.CreateClient();
        using var request = CreateRequest(
            second.GalleryToken,
            first.ParticipantToken,
            "Yetkisiz Değişiklik");

        var response = await client.SendAsync(request, cancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UpdateDisplayName_rejects_invalid_display_name(
        CancellationToken cancellationToken)
    {
        var seeded = await SeedParticipantAsync(uploadsEnabled: true, cancellationToken);
        using var client = _factory.CreateClient();
        using var request = CreateRequest(
            seeded.GalleryToken,
            seeded.ParticipantToken,
            "   ");

        var response = await client.SendAsync(request, cancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<SeededParticipant> SeedParticipantAsync(
        bool uploadsEnabled,
        CancellationToken cancellationToken)
    {
        var participantToken = $"participant-{Guid.NewGuid():N}";
        var participantTokenService = new ParticipantTokenService();
        var ownerId = Guid.CreateVersion7();
        var eventId = Guid.CreateVersion7();
        var participantId = Guid.CreateVersion7();
        var galleryToken = Guid.NewGuid().ToString("N");
        var now = DateTime.UtcNow;

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrtakareDbContext>();

        dbContext.Users.Add(new User
        {
            Id = ownerId,
            DisplayName = "Owner",
            Email = $"owner-{Guid.NewGuid():N}@example.com",
            NormalizedEmail = $"OWNER-{Guid.NewGuid():N}@EXAMPLE.COM",
            PasswordHash = "unused",
            CreatedAtUtc = now
        });

        dbContext.Events.Add(new Event
        {
            Id = eventId,
            OwnerUserId = ownerId,
            Title = "Participant Name Event",
            EventDateUtc = now.AddDays(1),
            GalleryToken = galleryToken,
            UploadsEnabled = uploadsEnabled,
            CreatedAtUtc = now
        });

        dbContext.EventGuestParticipants.Add(new EventGuestParticipant
        {
            Id = participantId,
            EventId = eventId,
            DisplayName = "Eski Rumuz",
            TokenHash = participantTokenService.Hash(participantToken),
            CreatedAtUtc = now
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        return new SeededParticipant(galleryToken, participantToken, participantId);
    }

    private static HttpRequestMessage CreateRequest(
        string galleryToken,
        string participantToken,
        string displayName)
    {
        var request = new HttpRequestMessage(
            HttpMethod.Put,
            $"/api/public/events/{galleryToken}/participants/me/display-name")
        {
            Content = JsonContent.Create(new UpdateParticipantDisplayNameRequest(displayName))
        };

        request.Headers.TryAddWithoutValidation("X-Participant-Token", participantToken);
        return request;
    }

    private sealed record SeededParticipant(
        string GalleryToken,
        string ParticipantToken,
        Guid ParticipantId);
}
