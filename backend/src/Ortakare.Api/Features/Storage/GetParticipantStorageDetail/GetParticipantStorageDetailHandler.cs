using Microsoft.EntityFrameworkCore;
using Ortakare.Api.Common;
using Ortakare.Api.Infrastructure.Authentication;
using Ortakare.Api.Infrastructure.ObjectStorage;
using Ortakare.Api.Infrastructure.Persistence;

namespace Ortakare.Api.Features.Storage.GetParticipantStorageDetail;

public sealed class GetParticipantStorageDetailHandler(
    OrtakareDbContext dbContext,
    ICurrentUser currentUser,
    IObjectStorageService objectStorageService,
    IConfiguration configuration,
    TimeProvider timeProvider)
{
    private const int TrendDayCount = 30;
    private const int RecentPhotoCount = 20;

    public async Task<ApiResult<GetParticipantStorageDetailResponse>> HandleAsync(
        Guid eventId,
        Guid participantId,
        CancellationToken cancellationToken)
    {
        var participantInfo = await (
            from participant in dbContext.EventGuestParticipants.AsNoTracking()
            join eventEntity in dbContext.Events.AsNoTracking()
                on participant.EventId equals eventEntity.Id
            where participant.Id == participantId &&
                  participant.EventId == eventId &&
                  eventEntity.OwnerUserId == currentUser.UserId
            select new
            {
                EventId = eventEntity.Id,
                EventTitle = eventEntity.Title,
                ParticipantId = participant.Id,
                participant.DisplayName,
                participant.IsBlocked,
                participant.BlockedAtUtc,
                JoinedAtUtc = participant.CreatedAtUtc
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (participantInfo is null)
        {
            return ApiResult<GetParticipantStorageDetailResponse>.Failure(
                "Katılımcı bulunamadı.",
                StatusCodes.Status404NotFound);
        }

        var aggregate = await dbContext.EventGuestPhotos
            .AsNoTracking()
            .Where(photo => photo.EventId == eventId && photo.ParticipantId == participantId)
            .GroupBy(_ => 1)
            .Select(group => new
            {
                PhotoCount = group.Count(),
                TotalStorageBytes = group.Sum(photo => photo.FileSizeBytes),
                FirstUploadAtUtc = (DateTime?)group.Min(photo => photo.CreatedAtUtc),
                LastUploadAtUtc = (DateTime?)group.Max(photo => photo.CreatedAtUtc)
            })
            .SingleOrDefaultAsync(cancellationToken);

        var todayUtc = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var firstDay = todayUtc.AddDays(-(TrendDayCount - 1));
        var firstDayUtc = firstDay.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        var groupedRows = await dbContext.EventGuestPhotos
            .AsNoTracking()
            .Where(photo =>
                photo.EventId == eventId &&
                photo.ParticipantId == participantId &&
                photo.CreatedAtUtc >= firstDayUtc)
            .GroupBy(photo => photo.CreatedAtUtc.Date)
            .Select(group => new
            {
                Date = group.Key,
                AddedBytes = group.Sum(photo => photo.FileSizeBytes),
                PhotoCount = group.Count()
            })
            .ToListAsync(cancellationToken);

        var valuesByDate = groupedRows.ToDictionary(
            row => DateOnly.FromDateTime(row.Date),
            row => (row.AddedBytes, row.PhotoCount));

        var days = Enumerable.Range(0, TrendDayCount)
            .Select(offset => firstDay.AddDays(offset))
            .Select(date =>
            {
                var values = valuesByDate.GetValueOrDefault(date);
                return new ParticipantStorageTrendDay(date, values.AddedBytes, values.PhotoCount);
            })
            .ToList();

        var recentPhotoRows = await dbContext.EventGuestPhotos
            .AsNoTracking()
            .Where(photo => photo.EventId == eventId && photo.ParticipantId == participantId)
            .OrderByDescending(photo => photo.CreatedAtUtc)
            .Take(RecentPhotoCount)
            .Select(photo => new
            {
                photo.Id,
                photo.OriginalFileName,
                photo.ContentType,
                photo.FileSizeBytes,
                photo.CreatedAtUtc,
                photo.StorageKey
            })
            .ToListAsync(cancellationToken);

        var signedUrlMinutes = configuration
            .GetSection(ObjectStorageOptions.SectionName)
            .GetValue<int?>(nameof(ObjectStorageOptions.SignedUrlMinutes)) ?? 10;
        var expiresAtUtc = timeProvider.GetUtcNow().UtcDateTime.AddMinutes(signedUrlMinutes);

        var recentPhotos = recentPhotoRows
            .Select(photo => new ParticipantRecentPhotoItem(
                photo.Id,
                photo.OriginalFileName,
                photo.ContentType,
                photo.FileSizeBytes,
                photo.CreatedAtUtc,
                objectStorageService.CreateReadUrl(photo.StorageKey, expiresAtUtc),
                expiresAtUtc))
            .ToList();

        var photoCount = aggregate?.PhotoCount ?? 0;
        var totalStorageBytes = aggregate?.TotalStorageBytes ?? 0;
        var averagePhotoSizeBytes = photoCount == 0 ? 0 : totalStorageBytes / photoCount;

        return ApiResult<GetParticipantStorageDetailResponse>.Success(
            new GetParticipantStorageDetailResponse(
                participantInfo.EventId,
                participantInfo.EventTitle,
                participantInfo.ParticipantId,
                participantInfo.DisplayName,
                participantInfo.IsBlocked,
                participantInfo.BlockedAtUtc,
                participantInfo.JoinedAtUtc,
                photoCount,
                totalStorageBytes,
                averagePhotoSizeBytes,
                aggregate?.FirstUploadAtUtc,
                aggregate?.LastUploadAtUtc,
                days,
                recentPhotos));
    }
}
