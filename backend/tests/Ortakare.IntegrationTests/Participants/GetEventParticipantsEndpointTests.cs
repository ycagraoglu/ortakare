using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Ortakare.Api.Common;
using Ortakare.Api.Features.Auth.Login;
using Ortakare.Api.Features.Auth.Register;
using Ortakare.Api.Features.Events;
using Ortakare.Api.Features.Participants;
using Ortakare.Api.Features.Participants.GetEventParticipants;
using Ortakare.Api.Features.Photos;
using Ortakare.Api.Infrastructure.Persistence;

namespace Ortakare.IntegrationTests.Participants;

public sealed class GetEventParticipantsEndpointTests : IClassFixture<OrtakareApiFactory>
{
    private readonly OrtakareApiFactory _factory;

    public GetEventParticipantsEndpointTests(OrtakareApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetEventParticipants_returns_owner_participants_with_photo_counts(
        CancellationToken cancellationToken)
    {
        using var client = _factory.CreateClient();
        var ownerId = await AuthenticateAsync(client, cancellationToken);
        var eventId = await SeedEventAsync(ownerId, cancellationToken);

        var response = await client.GetAsync(
            $"/api/events/{eventId}/participants?page=1&pageSize=1",
            cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        Assert.DoesNotContain("tokenHash", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("participantToken", json, StringComparison.OrdinalIgnoreCase);

        var result = await response.Content
            .ReadFromJsonAsync<ApiResult<GetEventParticipantsResponse>>(cancellationToken);

        Assert.NotNull(result?.Data);
        Assert.Single(result.Data.Items);
        Assert.Equal(2, result.Data.TotalCount);
        Assert.Equal(2, result.Data.TotalPages);
        Assert.Equal("Newest Guest", result.Data.Items[0].DisplayName);
        Assert.Equal(2, result.Data.Items[0].PhotoCount);
    }

    [Fact]
    public async Task GetEventParticipants_returns_not_found_for_another_owner(
        CancellationToken cancellationToken)
    {
        using var ownerClient = _factory.CreateClient();
        using var otherClient = _factory.CreateClient();
        var ownerId = await AuthenticateAsync(ownerClient, cancellationToken);
        await AuthenticateAsync(otherClient, cancellationToken);
        var eventId = await SeedEventAsync(ownerId, cancellationToken);

        var response = await otherClient.GetAsync(
            $"/api/events/{eventId}/participants",
            cancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetEventParticipants_rejects_invalid_paging(
        CancellationToken cancellationToken)
    {
        using var client = _factory.CreateClient();
        var ownerId = await AuthenticateAsync(client, cancellationToken);
        var eventId = await SeedEventAsync(ownerId, cancellationToken);

        var response = await client.GetAsync(
            $"/api/events/{eventId}/participants?page=0&pageSize=101",
            cancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<Guid> SeedEventAsync(Guid ownerId, CancellationToken cancellationToken)
    {
        var eventId = Guid.CreateVersion7();
        var olderParticipantId = Guid.CreateVersion7();
        var newerParticipantId = Guid.CreateVersion7();
        var now = DateTime.UtcNow;

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrtakareDbContext>();

        dbContext.Events.Add(new Event
        {
            Id = eventId,
            OwnerUserId = ownerId,
            Title = "Participant List Event",
            EventDateUtc = now.AddDays(1),
            GalleryToken = Guid.NewGuid().ToString("N"),
            UploadsEnabled = true,
            CreatedAtUtc = now
        });

        dbContext.EventGuestParticipants.AddRange(
            new EventGuestParticipant
            {
                Id = olderParticipantId,
                EventId = eventId,
                DisplayName = "Older Guest",
                TokenHash = Guid.NewGuid().ToString("N").PadRight(64, '0'),
                CreatedAtUtc = now.AddMinutes(-5)
            },
            new EventGuestParticipant
            {
                Id = newerParticipantId,
                EventId = eventId,
                DisplayName = "Newest Guest",
                TokenHash = Guid.NewGuid().ToString("N").PadRight(64, '1'),
                CreatedAtUtc = now
            });

        dbContext.EventGuestPhotos.AddRange(
            CreatePhoto(eventId, newerParticipantId, now),
            CreatePhoto(eventId, newerParticipantId, now.AddSeconds(1)),
            CreatePhoto(eventId, olderParticipantId, now.AddSeconds(2)));

        await dbContext.SaveChangesAsync(cancellationToken);
        return eventId;
    }

    private static EventGuestPhoto CreatePhoto(Guid eventId, Guid participantId, DateTime createdAtUtc) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            EventId = eventId,
            ParticipantId = participantId,
            ClientUploadId = Guid.NewGuid(),
            StorageKey = $"events/{eventId}/participants/{participantId}/{Guid.NewGuid():N}",
            OriginalFileName = "photo.jpg",
            ContentType = "image/jpeg",
            FileSizeBytes = 4,
            CreatedAtUtc = createdAtUtc
        };

    private static async Task<Guid> AuthenticateAsync(
        HttpClient client,
        CancellationToken cancellationToken)
    {
        var email = $"participant-owner-{Guid.NewGuid():N}@example.com";
        const string password = "StrongPassword123!";

        var registerResponse = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest("Participant Owner", email, password),
            cancellationToken);
        var registerResult = await registerResponse.Content
            .ReadFromJsonAsync<ApiResult<RegisterResponse>>(cancellationToken);

        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);
        Assert.NotNull(registerResult?.Data);

        var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(email, password),
            cancellationToken);
        var loginResult = await loginResponse.Content
            .ReadFromJsonAsync<ApiResult<LoginResponse>>(cancellationToken);

        Assert.NotNull(loginResult?.Data);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            loginResult.Data.AccessToken);

        return registerResult.Data.Id;
    }
}
