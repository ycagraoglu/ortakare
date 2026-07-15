using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Ortakare.Api.Common;
using Ortakare.Api.Infrastructure.Authentication;
using Ortakare.Api.Infrastructure.ObjectStorage;
using Ortakare.Api.Infrastructure.Persistence;

namespace Ortakare.Api.Features.Events.GetEventRecentActivity;

public sealed class GetEventRecentActivityHandler(
    OrtakareDbContext dbContext,
    ICurrentUser currentUser,
    IObjectStorageService objectStorageService,
    IOptions<ObjectStorageOptions> objectStorageOptions,
    TimeProvider timeProvider)
{
    public async Task<ApiResult<GetEventRecentActivityResponse>> HandleAsync(
        Guid eventId,
        GetEventRecentActivityRequest request,
        CancellationToken cancellationToken)
    {
        var ownsEvent = await dbContext.Events
            .AsNoTracking()
            .AnyAsync(
                x => x.Id == eventId && x.OwnerUserId == currentUser.UserId,
                cancellationToken);

        if (!ownsEvent)
        {
            return ApiResult<GetEventRecentActivityResponse>.Failure(
                "Etkinlik bulunamadı.",
                StatusCodes.Status404NotFound);
        }

        var recentParticipants = await dbContext.EventGuestParticipants
            .AsNoTracking()
            .Where(x => x.EventId == eventId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(request.Limit)
            .Select(x => new RecentParticipantItem(
                x.Id,
                x.DisplayName,
                x.IsBlocked,
                x.BlockedAtUtc,
                x.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        var recentPhotoRows = await dbContext.EventGuestPhotos
            .AsNoTracking()
            .Where(photo => photo.EventId == eventId)
            .Join(
                dbContext.EventGuestParticipants.AsNoTracking(),
                photo => photo.ParticipantId,
                participant => participant.Id,
                (photo, participant) => new
                {
                    PhotoId = photo.Id,
                    photo.ParticipantId,
                    ParticipantDisplayName = participant.DisplayName,
                    photo.ContentType,
                    photo.FileSizeBytes,
                    photo.CreatedAtUtc,
                    photo.StorageKey
                })
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(request.Limit)
            .ToListAsync(cancellationToken);

        var readUrlExpiresAtUtc = timeProvider.GetUtcNow()
            .AddMinutes(objectStorageOptions.Value.SignedUrlMinutes)
            .UtcDateTime;

        var recentPhotos = recentPhotoRows
            .Select(x => new RecentPhotoItem(
                x.PhotoId,
                x.ParticipantId,
                x.ParticipantDisplayName,
                x.ContentType,
                x.FileSizeBytes,
                x.CreatedAtUtc,
                objectStorageService.CreateReadUrl(x.StorageKey, readUrlExpiresAtUtc),
                readUrlExpiresAtUtc))
            .ToList();

        return ApiResult<GetEventRecentActivityResponse>.Success(
            new GetEventRecentActivityResponse(
                recentParticipants,
                recentPhotos));
    }
}
