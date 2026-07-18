using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Ortakare.Api.Common;
using Ortakare.Api.Features.Participants;
using Ortakare.Api.Features.Photos.DomainEvents;
using Ortakare.Api.Features.Storage;
using Ortakare.Api.Infrastructure.DomainEvents;
using Ortakare.Api.Infrastructure.ObjectStorage;
using Ortakare.Api.Infrastructure.Persistence;

namespace Ortakare.Api.Features.Photos.UploadPhoto;

public sealed class UploadPhotoHandler(
    OrtakareDbContext dbContext,
    ParticipantTokenService participantTokenService,
    ImageFileInspector imageFileInspector,
    StorageUploadPolicyService storageUploadPolicyService,
    IObjectStorageService objectStorageService,
    IOptions<PhotoUploadOptions> photoUploadOptions,
    TimeProvider timeProvider,
    IDomainEventDispatcher domainEventDispatcher)
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

        var file = request.File;
        if (file is null || file.Length <= 0)
        {
            return ApiResult<UploadPhotoResponse>.Failure(
                "Yüklenecek görsel dosyası zorunludur.",
                StatusCodes.Status400BadRequest);
        }

        var uploadOptions = photoUploadOptions.Value;
        if (file.Length > uploadOptions.MaxFileSizeBytes)
        {
            return ApiResult<UploadPhotoResponse>.Failure(
                "Görsel dosyası izin verilen boyut sınırını aşıyor.",
                StatusCodes.Status413PayloadTooLarge);
        }

        var safeOriginalFileName = Path.GetFileName(file.FileName ?? string.Empty);
        if (string.IsNullOrWhiteSpace(safeOriginalFileName) ||
            safeOriginalFileName.Length > uploadOptions.MaxOriginalFileNameLength ||
            safeOriginalFileName.Any(char.IsControl))
        {
            return ApiResult<UploadPhotoResponse>.Failure(
                "Görsel dosya adı geçersiz.",
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
                eventEntity.OwnerUserId,
                participant.IsBlocked,
                eventEntity.UploadsEnabled
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (participantInfo is null)
        {
            return ApiResult<UploadPhotoResponse>.Failure(
                "Katılımcı doğrulanamadı.",
                StatusCodes.Status401Unauthorized);
        }

        if (participantInfo.IsBlocked)
        {
            return ApiResult<UploadPhotoResponse>.Failure(
                "Bu katılımcının yükleme erişimi engellendi.",
                StatusCodes.Status403Forbidden);
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

        var uploadDecision = await storageUploadPolicyService.EvaluateAsync(
            participantInfo.OwnerUserId,
            participantInfo.UploadsEnabled,
            1,
            file.Length,
            file.Length,
            cancellationToken);

        if (!uploadDecision.CanUpload)
        {
            return ApiResult<UploadPhotoResponse>.Failure(
                uploadDecision.Message ?? "Fotoğraf yükleme kuralları karşılanmadı.",
                StatusCodes.Status409Conflict);
        }

        await using var stream = file.OpenReadStream();
        var imageInfo = await imageFileInspector.InspectAsync(stream, cancellationToken);

        if (imageInfo is null)
        {
            return ApiResult<UploadPhotoResponse>.Failure(
                "Dosya içeriği desteklenen ve geçerli bir görsel formatı değil.",
                StatusCodes.Status415UnsupportedMediaType);
        }

        if (!string.IsNullOrWhiteSpace(imageInfo.ValidationError))
        {
            return ApiResult<UploadPhotoResponse>.Failure(
                imageInfo.ValidationError,
                StatusCodes.Status422UnprocessableEntity);
        }

        if (!IsCompatibleDeclaredContentType(file.ContentType, imageInfo.ContentType))
        {
            return ApiResult<UploadPhotoResponse>.Failure(
                "Dosyanın bildirilen içerik türü ile gerçek görsel formatı uyuşmuyor.",
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

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var photo = new EventGuestPhoto
        {
            Id = photoId,
            EventId = participantInfo.EventId,
            ParticipantId = participantInfo.ParticipantId,
            ClientUploadId = clientUploadId,
            StorageKey = storageKey,
            OriginalFileName = safeOriginalFileName,
            ContentType = imageInfo.ContentType,
            FileSizeBytes = file.Length,
            CreatedAtUtc = now
        };

        dbContext.EventGuestPhotos.Add(photo);

        await domainEventDispatcher.PublishAsync(
            new PhotoUploadedDomainEvent(
                participantInfo.EventId,
                participantInfo.OwnerUserId,
                participantInfo.ParticipantId,
                photo.Id,
                photo.FileSizeBytes,
                now),
            cancellationToken);

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

    private static bool IsCompatibleDeclaredContentType(string? declared, string detected)
    {
        if (string.IsNullOrWhiteSpace(declared) ||
            declared.Equals("application/octet-stream", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (declared.Equals(detected, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return detected == "image/jpeg" &&
               declared.Equals("image/jpg", StringComparison.OrdinalIgnoreCase);
    }
}