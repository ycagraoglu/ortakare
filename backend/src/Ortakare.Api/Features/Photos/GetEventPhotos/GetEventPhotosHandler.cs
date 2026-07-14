using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Ortakare.Api.Common;
using Ortakare.Api.Infrastructure.Authentication;
using Ortakare.Api.Infrastructure.ObjectStorage;
using Ortakare.Api.Infrastructure.Persistence;

namespace Ortakare.Api.Features.Photos.GetEventPhotos;

public sealed class GetEventPhotosHandler(
    OrtakareDbContext dbContext,
    ICurrentUser currentUser,
    IObjectStorageService objectStorageService,
    IOptions<ObjectStorageOptions> objectStorageOptions,
    TimeProvider timeProvider)
{
    public async Task<ApiResult<GetEventPhotosResponse>> HandleAsync(
        Guid eventId,
        GetEventPhotosRequest request,
        CancellationToken cancellationToken)
    {
        var ownsEvent = await dbContext.Events
            .AsNoTracking()
            .AnyAsync(x => x.Id == eventId && x.OwnerUserId == currentUser.UserId, cancellationToken);

        if (!ownsEvent)
        {
            return ApiResult<GetEventPhotosResponse>.Failure(
                "Etkinlik bulunamadı.",
                StatusCodes.Status404NotFound);
        }

        var query = dbContext.EventGuestPhotos
            .AsNoTracking()
            .Where(x => x.EventId == eventId);

        var totalCount = await query.CountAsync(cancellationToken);

        var rows = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Join(
                dbContext.EventGuestParticipants.AsNoTracking(),
                photo => photo.ParticipantId,
                participant => participant.Id,
                (photo, participant) => new
                {
                    photo.Id,
                    participant.DisplayName,
                    photo.ContentType,
                    photo.FileSizeBytes,
                    photo.CreatedAtUtc,
                    photo.StorageKey
                })
            .ToListAsync(cancellationToken);

        var expiresAtUtc = timeProvider.GetUtcNow()
            .AddMinutes(objectStorageOptions.Value.SignedUrlMinutes)
            .UtcDateTime;

        var items = rows
            .Select(x => new GetEventPhotosItem(
                x.Id,
                x.DisplayName,
                x.ContentType,
                x.FileSizeBytes,
                x.CreatedAtUtc,
                objectStorageService.CreateReadUrl(x.StorageKey, expiresAtUtc),
                expiresAtUtc))
            .ToList();

        var totalPages = totalCount == 0
            ? 0
            : (int)Math.Ceiling(totalCount / (double)request.PageSize);

        return ApiResult<GetEventPhotosResponse>.Success(
            new GetEventPhotosResponse(
                items,
                request.Page,
                request.PageSize,
                totalCount,
                totalPages));
    }
}