using Microsoft.EntityFrameworkCore;
using Ortakare.Api.Common;
using Ortakare.Api.Features.Participants;
using Ortakare.Api.Infrastructure.ObjectStorage;
using Ortakare.Api.Infrastructure.Persistence;

namespace Ortakare.Api.Features.Photos.UploadPhoto;

public sealed class UploadPhotoHandler(
    OrtakareDbContext dbContext,
    ParticipantTokenService participantTokenService,
    ImageFileInspector imageFileInspector,
    IObjectStorageService objectStorageService,
    TimeProvider timeProvider)
{
    public async Task<ApiResult<UploadPhotoResponse>> HandleAsync(
        string galleryToken,
        string participantToken,
        Guid clientUploadId,
        UploadPhotoRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(participantToken))
        {
            return ApiResult<UploadPhotoResponse>.Failure(
                "Katılımcı tokenı zorunludur.",
                StatusCodes.Status401Unauthorized);
        }

        if (clientUploadId == Guid.Empty)
        {
            return ApiResult<UploadPhotoResponse>.Failure(
                "Geçerli bir client upload ID gönderilmelidir.",
                StatusCodes.Status400BadRequest);
        }

        var participantTokenHash = participantTokenService.Hash(participantToken);

        var participantInfo = await (
            from participant in dbContext.EventGuestParticipants.AsNoTracking()
            join eventEntity in dbContext.Events.AsNoTracking()
                on participant.EventId equals eventEntity.Id
            where participant.TokenHash == participantTokenHash &&
                  eventEntity.GalleryToken == galleryToken
            select new
            {
                ParticipantId = participant.Id,
                EventId = eventEntity.Id,
                eventEntity.UploadsEnabled
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (participantInfo is null)
        {
            return ApiResult<UploadPhotoResponse>.Failure(
                "Katılımcı doğrulanamadı.",
                StatusCodes.Status401Unauthorized);
        }

        if (!participantInfo.UploadsEnabled)
        {
            return ApiResult<UploadPhotoResponse>.Failure(
                "Bu albüm yeni yüklemelere kapatıldı.",
                StatusCodes.Status409Conflict);
        }

        var existingPhoto = await dbContext.EventGuestPhotos
            .AsNoTracking()
            .Where(x => x.ParticipantId == participantInfo.ParticipantId &&
                        x.ClientUploadId == clientUploadId)
            .Select(x => new UploadPhotoResponse(
                x.Id,
                x.ClientUploadId,
                x.ContentType,
                x.FileSizeBytes,
                x.CreatedAtUtc,
                true))
            .SingleOrDefaultAsync(cancellationToken);

        if (existingPhoto is not null)
        {
            return ApiResult<UploadPhotoResponse>.Success(existingPhoto);
        }

        var file = request.File!;
        await using var stream = file.OpenReadStream();
        var imageInfo = await imageFileInspector.InspectAsync(stream, cancellationToken);

        if (imageInfo is null)
        {
            return ApiResult<UploadPhotoResponse>.Failure(
                "Desteklenmeyen veya geçersiz görsel dosyası.",
                StatusCodes.Status415UnsupportedMediaType);
        }

        var photoId = Guid.CreateVersion7();
        var storageKey = $"events/{participantInfo.EventId:N}/participants/{participantInfo.ParticipantId:N}/{clientUploadId:N}";

        await objectStorageService.UploadAsync(
            storageKey,
            stream,
            imageInfo.ContentType,
            file.Length,
            cancellationToken);

        var photo = new EventGuestPhoto
        {
            Id = photoId,
            EventId = participantInfo.EventId,
            ParticipantId = participantInfo.ParticipantId,
            ClientUploadId = clientUploadId,
            StorageKey = storageKey,
            OriginalFileName = Path.GetFileName(file.FileName),
            ContentType = imageInfo.ContentType,
            FileSizeBytes = file.Length,
            CreatedAtUtc = timeProvider.GetUtcNow().UtcDateTime
        };

        dbContext.EventGuestPhotos.Add(photo);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await objectStorageService.DeleteAsync(storageKey, CancellationToken.None);
            throw;
        }

        return ApiResult<UploadPhotoResponse>.Success(
            new UploadPhotoResponse(
                photo.Id,
                photo.ClientUploadId,
                photo.ContentType,
                photo.FileSizeBytes,
                photo.CreatedAtUtc,
                false),
            "Fotoğraf yüklendi.",
            StatusCodes.Status201Created);
    }
}