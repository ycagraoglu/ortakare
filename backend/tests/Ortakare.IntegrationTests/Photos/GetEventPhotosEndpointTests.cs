using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Ortakare.Api.Common;
using Ortakare.Api.Features.Auth.Login;
using Ortakare.Api.Features.Auth.Register;
using Ortakare.Api.Features.Events;
using Ortakare.Api.Features.Photos;
using Ortakare.Api.Features.Photos.GetEventPhotos;
using Ortakare.Api.Features.Participants;
using Ortakare.Api.Infrastructure.Persistence;

namespace Ortakare.IntegrationTests.Photos;

public sealed class GetEventPhotosEndpointTests : IClassFixture<OrtakareApiFactory>
{
    private readonly OrtakareApiFactory _factory;

    public GetEventPhotosEndpointTests(OrtakareApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetEventPhotos_returns_owned_event_photos_with_signed_urls(
        CancellationToken cancellationToken)
    {
        using var ownerClient = _factory.CreateClient();
        var ownerId = await AuthenticateAsync(ownerClient, cancellationToken);
        var eventId = Guid.CreateVersion7();
        var participantId = Guid.CreateVersion7();

        await SeedPhotoAsync(ownerId, eventId, participantId, cancellationToken);

        var response = await ownerClient.GetAsync(
            $"/api/events/{eventId}/photos?page=1&pageSize=30",
            cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<ApiResult<GetEventPhotosResponse>>(
            cancellationToken);

        Assert.NotNull(result?.Data);
        Assert.Single(result.Data.Items);
        Assert.Equal("Wedding Guest", result.Data.Items[0].ParticipantDisplayName);
        Assert.StartsWith("https://storage.test/", result.Data.Items[0].SignedUrl);
        Assert.True(result.Data.Items[0].SignedUrlExpiresAtUtc > DateTime.UtcNow);
    }

    [Fact]
    public async Task GetEventPhotos_returns_not_found_for_another_users_event(
        CancellationToken cancellationToken)
    {
        using var ownerClient = _factory.CreateClient();
        using var otherClient = _factory.CreateClient();
        var ownerId = await AuthenticateAsync(ownerClient, cancellationToken);
        await AuthenticateAsync(otherClient, cancellationToken);
        var eventId = Guid.CreateVersion7();

        await SeedEventAsync(ownerId, eventId, cancellationToken);

        var response = await otherClient.GetAsync(
            $"/api/events/{eventId}/photos",
            cancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task SeedPhotoAsync(
        Guid ownerId,
        Guid eventId,
        Guid participantId,
        CancellationToken cancellationToken)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrtakareDbContext>();

        dbContext.Events.Add(new Event
        {
            Id = eventId,
            OwnerUserId = ownerId,
            Title = "Wedding",
            EventDateUtc = new DateTime(2027, 9, 10, 18, 0, 0, DateTimeKind.Utc),
            GalleryToken = Guid.NewGuid().ToString("N"),
            UploadsEnabled = true,
            CreatedAtUtc = DateTime.UtcNow
        });

        dbContext.EventGuestParticipants.Add(new EventGuestParticipant
        {
            Id = participantId,
            EventId = eventId,
            DisplayName = "Wedding Guest",
            TokenHash = Guid.NewGuid().ToString("N").PadRight(64, '0'),
            CreatedAtUtc = DateTime.UtcNow
        });

        dbContext.EventGuestPhotos.Add(new EventGuestPhoto
        {
            Id = Guid.CreateVersion7(),
            EventId = eventId,
            ParticipantId = participantId,
            ClientUploadId = Guid.NewGuid(),
            StorageKey = $"events/{eventId}/photo",
            OriginalFileName = "photo.jpg",
            ContentType = "image/jpeg",
            FileSizeBytes = 1234,
            CreatedAtUtc = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedEventAsync(Guid ownerId, Guid eventId, CancellationToken cancellationToken)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrtakareDbContext>();
        dbContext.Events.Add(new Event
        {
            Id = eventId,
            OwnerUserId = ownerId,
            Title = "Private Event",
            EventDateUtc = new DateTime(2027, 9, 10, 18, 0, 0, DateTimeKind.Utc),
            GalleryToken = Guid.NewGuid().ToString("N"),
            UploadsEnabled = true,
            CreatedAtUtc = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task<Guid> AuthenticateAsync(
        HttpClient client,
        CancellationToken cancellationToken)
    {
        var email = $"photo-list-{Guid.NewGuid():N}@example.com";
        const string password = "StrongPassword123!";

        await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest("Photo Owner", email, password),
            cancellationToken);

        var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(email, password),
            cancellationToken);

        var loginResult = await loginResponse.Content.ReadFromJsonAsync<ApiResult<LoginResponse>>(
            cancellationToken);

        Assert.NotNull(loginResult?.Data);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            loginResult.Data.AccessToken);

        return loginResult.Data.UserId;
    }
}