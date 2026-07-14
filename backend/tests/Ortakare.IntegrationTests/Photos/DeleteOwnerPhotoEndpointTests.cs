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
using Ortakare.Api.Features.Photos;
using Ortakare.Api.Features.Photos.DeleteOwnerPhoto;
using Ortakare.Api.Infrastructure.Persistence;

namespace Ortakare.IntegrationTests.Photos;

public sealed class DeleteOwnerPhotoEndpointTests : IClassFixture<OrtakareApiFactory>
{
    private readonly OrtakareApiFactory _factory;

    public DeleteOwnerPhotoEndpointTests(OrtakareApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task DeleteOwnerPhoto_removes_storage_object_and_database_record(
        CancellationToken cancellationToken)
    {
        _factory.ObjectStorage.Reset();
        using var client = _factory.CreateClient();
        var ownerId = await AuthenticateAsync(client, cancellationToken);
        var seeded = await SeedPhotoAsync(ownerId, cancellationToken);

        var response = await client.DeleteAsync(
            $"/api/events/{seeded.EventId}/photos/{seeded.PhotoId}",
            cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, _factory.ObjectStorage.DeleteCount);
        Assert.DoesNotContain(seeded.StorageKey, _factory.ObjectStorage.Objects.Keys);

        var result = await response.Content
            .ReadFromJsonAsync<ApiResult<DeleteOwnerPhotoResponse>>(cancellationToken);
        Assert.Equal(seeded.PhotoId, result?.Data?.PhotoId);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrtakareDbContext>();
        Assert.False(await dbContext.EventGuestPhotos.AnyAsync(
            x => x.Id == seeded.PhotoId,
            cancellationToken));
    }

    [Fact]
    public async Task DeleteOwnerPhoto_returns_not_found_for_another_owner(
        CancellationToken cancellationToken)
    {
        _factory.ObjectStorage.Reset();
        using var ownerClient = _factory.CreateClient();
        using var otherClient = _factory.CreateClient();
        var ownerId = await AuthenticateAsync(ownerClient, cancellationToken);
        await AuthenticateAsync(otherClient, cancellationToken);
        var seeded = await SeedPhotoAsync(ownerId, cancellationToken);

        var response = await otherClient.DeleteAsync(
            $"/api/events/{seeded.EventId}/photos/{seeded.PhotoId}",
            cancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(0, _factory.ObjectStorage.DeleteCount);
    }

    [Fact]
    public async Task DeleteOwnerPhoto_preserves_database_record_when_storage_delete_fails(
        CancellationToken cancellationToken)
    {
        _factory.ObjectStorage.Reset();
        using var client = _factory.CreateClient();
        var ownerId = await AuthenticateAsync(client, cancellationToken);
        var seeded = await SeedPhotoAsync(ownerId, cancellationToken);
        _factory.ObjectStorage.ThrowOnDelete = true;

        var response = await client.DeleteAsync(
            $"/api/events/{seeded.EventId}/photos/{seeded.PhotoId}",
            cancellationToken);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrtakareDbContext>();
        Assert.True(await dbContext.EventGuestPhotos.AnyAsync(
            x => x.Id == seeded.PhotoId,
            cancellationToken));
    }

    private async Task<SeededPhoto> SeedPhotoAsync(
        Guid ownerId,
        CancellationToken cancellationToken)
    {
        var eventId = Guid.CreateVersion7();
        var participantId = Guid.CreateVersion7();
        var photoId = Guid.CreateVersion7();
        var storageKey = $"events/{eventId}/participants/{participantId}/{Guid.NewGuid()}";
        var now = DateTime.UtcNow;

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrtakareDbContext>();

        dbContext.Events.Add(new Event
        {
            Id = eventId,
            OwnerUserId = ownerId,
            Title = "Delete Photo Event",
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

        dbContext.EventGuestPhotos.Add(new EventGuestPhoto
        {
            Id = photoId,
            EventId = eventId,
            ParticipantId = participantId,
            ClientUploadId = Guid.NewGuid(),
            StorageKey = storageKey,
            OriginalFileName = "photo.jpg",
            ContentType = "image/jpeg",
            FileSizeBytes = 4,
            CreatedAtUtc = now
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        await _factory.ObjectStorage.UploadAsync(
            storageKey,
            new MemoryStream(new byte[] { 0xFF, 0xD8, 0xFF, 0xD9 }),
            "image/jpeg",
            4,
            cancellationToken);

        return new SeededPhoto(eventId, photoId, storageKey);
    }

    private static async Task<Guid> AuthenticateAsync(
        HttpClient client,
        CancellationToken cancellationToken)
    {
        var email = $"delete-photo-{Guid.NewGuid():N}@example.com";
        const string password = "StrongPassword123!";

        var registerResponse = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest("Photo Owner", email, password),
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

    private sealed record SeededPhoto(Guid EventId, Guid PhotoId, string StorageKey);
}