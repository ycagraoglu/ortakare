using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Ortakare.Api.Common;
using Ortakare.Api.Features.Auth.Login;
using Ortakare.Api.Features.Auth.Register;
using Ortakare.Api.Features.Events;
using Ortakare.Api.Features.GalleryExports;
using Ortakare.Api.Features.GalleryExports.CreateGalleryExport;
using Ortakare.Api.Features.Participants;
using Ortakare.Api.Features.Photos;
using Ortakare.Api.Infrastructure.Persistence;

namespace Ortakare.IntegrationTests.GalleryExports;

public sealed class CreateGalleryExportEndpointTests : IClassFixture<OrtakareApiFactory>
{
    private readonly OrtakareApiFactory _factory;

    public CreateGalleryExportEndpointTests(OrtakareApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateGalleryExport_creates_pending_export_with_photo_count(
        CancellationToken cancellationToken)
    {
        using var client = _factory.CreateClient();
        var ownerId = await AuthenticateAsync(client, cancellationToken);
        var eventId = await SeedEventAsync(ownerId, photoCount: 2, cancellationToken);

        var response = await client.PostAsync($"/api/events/{eventId}/exports", null, cancellationToken);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var result = await response.Content
            .ReadFromJsonAsync<ApiResult<CreateGalleryExportResponse>>(cancellationToken);

        Assert.NotNull(result?.Data);
        Assert.Equal(GalleryExportStatus.Pending, result.Data.Status);
        Assert.Equal(2, result.Data.PhotoCount);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrtakareDbContext>();
        var galleryExport = await dbContext.GalleryExports
            .AsNoTracking()
            .SingleAsync(x => x.Id == result.Data.ExportId, cancellationToken);

        Assert.Equal(GalleryExportStatus.Pending, galleryExport.Status);
        Assert.Null(galleryExport.StorageKey);
    }

    [Fact]
    public async Task CreateGalleryExport_returns_conflict_when_event_has_no_photos(
        CancellationToken cancellationToken)
    {
        using var client = _factory.CreateClient();
        var ownerId = await AuthenticateAsync(client, cancellationToken);
        var eventId = await SeedEventAsync(ownerId, photoCount: 0, cancellationToken);

        var response = await client.PostAsync($"/api/events/{eventId}/exports", null, cancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task CreateGalleryExport_returns_not_found_for_another_owner(
        CancellationToken cancellationToken)
    {
        using var ownerClient = _factory.CreateClient();
        using var otherClient = _factory.CreateClient();
        var ownerId = await AuthenticateAsync(ownerClient, cancellationToken);
        await AuthenticateAsync(otherClient, cancellationToken);
        var eventId = await SeedEventAsync(ownerId, photoCount: 1, cancellationToken);

        var response = await otherClient.PostAsync($"/api/events/{eventId}/exports", null, cancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<Guid> SeedEventAsync(
        Guid ownerId,
        int photoCount,
        CancellationToken cancellationToken)
    {
        var eventId = Guid.CreateVersion7();
        var participantId = Guid.CreateVersion7();
        var now = DateTime.UtcNow;

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrtakareDbContext>();

        dbContext.Events.Add(new Event
        {
            Id = eventId,
            OwnerUserId = ownerId,
            Title = "Export Event",
            EventDateUtc = now.AddDays(1),
            GalleryToken = Guid.NewGuid().ToString("N"),
            UploadsEnabled = true,
            CreatedAtUtc = now
        });

        dbContext.EventGuestParticipants.Add(new EventGuestParticipant
        {
            Id = participantId,
            EventId = eventId,
            DisplayName = "Guest",
            TokenHash = Guid.NewGuid().ToString("N").PadRight(64, '0'),
            CreatedAtUtc = now
        });

        for (var index = 0; index < photoCount; index++)
        {
            dbContext.EventGuestPhotos.Add(new EventGuestPhoto
            {
                Id = Guid.CreateVersion7(),
                EventId = eventId,
                ParticipantId = participantId,
                ClientUploadId = Guid.NewGuid(),
                StorageKey = $"events/{eventId}/participants/{participantId}/{Guid.NewGuid()}",
                OriginalFileName = $"photo-{index}.jpg",
                ContentType = "image/jpeg",
                FileSizeBytes = 4,
                CreatedAtUtc = now.AddSeconds(index)
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return eventId;
    }

    private static async Task<Guid> AuthenticateAsync(
        HttpClient client,
        CancellationToken cancellationToken)
    {
        var email = $"export-{Guid.NewGuid():N}@example.com";
        const string password = "StrongPassword123!";

        var registerResponse = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest("Export Owner", email, password),
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
