using Microsoft.EntityFrameworkCore;
using Ortakare.Api.Common;
using Ortakare.Api.Features.GalleryExports;
using Ortakare.Api.Infrastructure.Authentication;
using Ortakare.Api.Infrastructure.Persistence;

namespace Ortakare.Api.Features.Events.GetEventSummary;

public sealed class GetEventSummaryHandler(
    OrtakareDbContext dbContext,
    ICurrentUser currentUser)
{
    public async Task<ApiResult<GetEventSummaryResponse>> HandleAsync(
        Guid eventId,
        CancellationToken cancellationToken)
    {
        var eventInfo = await dbContext.Events
            .AsNoTracking()
            .Where(x => x.Id == eventId && x.OwnerUserId == currentUser.UserId)
            .Select(x => new
            {
                x.Id,
                x.Title,
                x.EventDateUtc,
                x.UploadsEnabled
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (eventInfo is null)
        {
            return ApiResult<GetEventSummaryResponse>.Failure(
                "Etkinlik bulunamadı.",
                StatusCodes.Status404NotFound);
        }

        var participantSummary = await dbContext.EventGuestParticipants
            .AsNoTracking()
            .Where(x => x.EventId == eventId)
            .GroupBy(_ => 1)
            .Select(group => new
            {
                TotalCount = group.Count(),
                BlockedCount = group.Count(x => x.IsBlocked)
            })
            .SingleOrDefaultAsync(cancellationToken);

        var photoSummary = await dbContext.EventGuestPhotos
            .AsNoTracking()
            .Where(x => x.EventId == eventId)
            .GroupBy(_ => 1)
            .Select(group => new
            {
                TotalCount = group.Count(),
                TotalSizeBytes = group.Sum(x => x.FileSizeBytes)
            })
            .SingleOrDefaultAsync(cancellationToken);

        var exportCounts = await dbContext.GalleryExports
            .AsNoTracking()
            .Where(x => x.EventId == eventId)
            .GroupBy(x => x.Status)
            .Select(group => new
            {
                Status = group.Key,
                Count = group.Count()
            })
            .ToListAsync(cancellationToken);

        var countsByStatus = exportCounts.ToDictionary(x => x.Status, x => x.Count);
        var exportSummary = new GalleryExportSummary(
            exportCounts.Sum(x => x.Count),
            GetCount(GalleryExportStatus.Pending),
            GetCount(GalleryExportStatus.Processing),
            GetCount(GalleryExportStatus.Completed),
            GetCount(GalleryExportStatus.Failed),
            GetCount(GalleryExportStatus.Cancelled));

        return ApiResult<GetEventSummaryResponse>.Success(
            new GetEventSummaryResponse(
                eventInfo.Id,
                eventInfo.Title,
                eventInfo.EventDateUtc,
                eventInfo.UploadsEnabled,
                participantSummary?.TotalCount ?? 0,
                participantSummary?.BlockedCount ?? 0,
                photoSummary?.TotalCount ?? 0,
                photoSummary?.TotalSizeBytes ?? 0,
                exportSummary));

        int GetCount(GalleryExportStatus status) =>
            countsByStatus.GetValueOrDefault(status);
    }
}
