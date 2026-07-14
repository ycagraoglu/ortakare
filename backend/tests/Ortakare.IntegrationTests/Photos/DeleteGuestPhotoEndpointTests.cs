using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Ortakare.Api.Common;
using Ortakare.Api.Features.Events;
using Ortakare.Api.Features.Participants;
using Ortakare.Api.Features.Photos;
using Ortakare.Api.Features.Photos.DeleteGuestPhoto;
using Ortakare.Api.Features.Users;
using Ortakare.Api.Infrastructure.Persistence;

namespace Ortakare.IntegrationTests.Photos;

public sealed class DeleteGuestPhotoEndpointTests : IClassFixture<OrtakareApiFactory>
{
    private readonly OrtakareApiFactory _factory;

    public DeleteGuestPhotoEndpointTests(OrtakareApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task DeleteGuestPhoto_removes_own_photo_from_storage_and_database(
        CancellationToken cancellationToken)
    {
        _factory.ObjectStorage.Reset();
        var seeded = await SeedPhotoAsync(cancellationToken);
        using var client = _factory.CreateClient();
        using var request = CreateDeleteRequest(
            seeded.GalleryToken,
            seeded.OwnerParticipantToken,
            seeded.PhotoId);

        var response = await client.SendAsync(request, cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, _factory.ObjectStorage.DeleteCount);
        Assert.DoesNotContain(seeded.StorageKey, _factory.ObjectStorage.Objects.Keys);

        var result = await response.Content
            .ReadFromJsonAsync<ApiResult<DeleteGuestPhotoResponse>>(cancellationToken);
        Assert.Equal(seeded.PhotoId, result?.Data?.PhotoId);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrtakareDbContext>();
        Assert.False(await dbContext.EventGuestPhotos.AnyAsync(
            x => x.Id == seeded.PhotoId,
            cancellationToken));
    }

    [Fact]
    public async Task DeleteGuestPhoto_returns_not_found_for_another_participants_photo(
        CancellationToken cancellationToken)
    {
        _factory.ObjectStorage.Reset();
        var seeded = await SeedPhotoAsync(cancellationToken);
        using var client = _factory.CreateClient();
        using var request = CreateDeleteRequest(
            seeded.GalleryToken,
            seeded.OtherParticipantToken,
            seeded.PhotoId);

        var response = await client.SendAsync(request, cancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(0, _factory.ObjectStorage.DeleteCount);
    }

    [Fact]
    public async Task DeleteGuestPhoto_returns_unauthorized_for_invalid_participant_token(
        CancellationToken cancellationToken)
    {
        _factory.ObjectStorage.Reset();
        var seeded = await SeedPhotoAsync(cancellationToken);
        using var client = _factory.CreateClient();
        using var request = CreateDeleteRequest(
            seeded.GalleryToken,
            "invalid-participant-token",
            seeded.PhotoId);

        var response = await client.SendAsync(request, cancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(0, _factory.ObjectStorage.DeleteCount);
    }

    [Fact]
    public async Task DeleteGuestPhoto_preserves_database_record_when_storage_delete_fails(
        CancellationToken cancellationToken)
    {
        _factory.ObjectStorage.Reset();
        var seeded = await SeedPhotoAsync(cancellationToken);
        _factory.ObjectStorage.ThrowOnDelete = true;
        using var client = _factory.CreateClient();
        using var request = CreateDeleteRequest(
            seeded.GalleryToken,
            seeded.OwnerParticipantToken,
            seeded.PhotoId);

        var response = await client.SendAsync(request, cancellationToken);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrtakareDbContext>();
        Assert.True(await dbContext.EventGuestPhotos.AnyAsync(
            x => x.Id == seeded.PhotoId,
            cancellationToken));
    }

    private async Task<SeededGuestPhoto> SeedPhotoAsync(CancellationToken cancellationToken)
    {
        var ownerParticipantToken = $"owner-{Guid.NewGuid():N}";
        var otherParticipantToken = $"other-{Guid.NewGuid():N}";
        var ownerUserId = Guid.CreateVersion7();
        var eventId = Guid.CreateVersion7();
        var ownerParticipantId = Guid.CreateVersion7();
        var otherParticipantId = Guid.CreateVersion7();
        var photoId = Guid.CreateVersion7();
        var galleryToken = Guid.NewGuid().ToString("N");
        var storageKey = $"events/{eventId}/participants/{ownerParticipantId}/{Guid.NewGuid()}";
        var now = DateTime.UtcNow;
        var tokenService = new ParticipantTokenService();
        var email = $"guest-delete-{Guid.NewGuid():N}@example.com";

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrtakareDbContext>();

        dbContext.Users.Add(new User
        {
            Id = ownerUserId,
            DisplayName = "Guest Delete Owner",
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            PasswordHash = "not-used-in-this-test",
            CreatedAtUtc = now
        });

        dbContext.Events.Add(new Event
        {
            Id = eventId,
            OwnerUserId = ownerUserId,
            Title = "Guest Delete Event",
            EventDateUtc = now.AddDays(1),
            GalleryToken = galleryToken,
            UploadsEnabled = false,
            CreatedAtUtc = now
        });

        dbContext.EventGuestParticipants.AddRange(
            new EventGuestParticipant
            {
                Id = ownerParticipantId,
                EventId = eventId,
                DisplayName = "Owner Guest",
                TokenHash = tokenService.Hash(ownerParticipantToken),
                CreatedAtUtc = now
            },
            new EventGuestParticipant
            {
                Id = otherParticipantId,
                EventId = eventId,
                DisplayName = "Other Guest",
                TokenHash = tokenService.Hash(otherParticipantToken),
                CreatedAtUtc = now
            });

        dbContext.EventGuestPhotos.Add(new EventGuestPhoto
        {
            Id = photoId,
            EventId = eventId,
            ParticipantId = ownerParticipantId,
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

        return new SeededGuestPhoto(
            galleryToken,
            ownerParticipantToken,
            otherParticipantToken,
            photoId,
            storageKey);
    }

    private static HttpRequestMessage CreateDeleteRequest(
        string galleryToken,
        string participantToken,
        Guid photoId)
    {
        var request = new HttpRequestMessage(
            HttpMethod.Delete,
            $"/api/public/events/{galleryToken}/photos/{photoId}");
        request.Headers.TryAddWithoutValidation("X-Participant-Token", participantToken);
        return request;
    }

    private sealed record SeededGuestPhoto(
        string GalleryToken,
        string OwnerParticipantToken,
        string OtherParticipantToken,
        Guid PhotoId,
        string StorageKey);
}
