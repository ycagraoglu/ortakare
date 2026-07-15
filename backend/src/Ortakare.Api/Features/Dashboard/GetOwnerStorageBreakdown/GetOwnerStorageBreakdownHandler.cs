using Microsoft.EntityFrameworkCore;
using Ortakare.Api.Common;
using Ortakare.Api.Infrastructure.Authentication;
using Ortakare.Api.Infrastructure.Persistence;

namespace Ortakare.Api.Features.Dashboard.GetOwnerStorageBreakdown;

public sealed class GetOwnerStorageBreakdownHandler(
    OrtakareDbContext dbContext,
    ICurrentUser currentUser)
{
    public async Task<ApiResult<GetOwnerStorageBreakdownResponse>> HandleAsync(
        GetOwnerStorageBreakdownRequest request,
        CancellationToken cancellationToken)
    {
        var eventAggregates = dbContext.Events
            .AsNoTracking()
            .Where(eventEntity => eventEntity.OwnerUserId == currentUser.UserId)
            .Select(eventEntity => new
            {
                Event = eventEntity,
                PhotoCount = dbContext.EventGuestPhotos.Count(photo => photo.EventId == eventEntity.Id),
                TotalStorageBytes = dbContext.EventGuestPhotos
                    .Where(photo => photo.EventId == eventEntity.Id)
                    .Sum(photo => (long?)photo.FileSizeBytes) ?? 0,
                LastUploadAtUtc = dbContext.EventGuestPhotos
                    .Where(photo => photo.EventId == eventEntity.Id)
                    .Max(photo => (DateTime?)photo.CreatedAtUtc)
            });

        var totalEventCount = await eventAggregates.CountAsync(cancellationToken);

        var ownerPhotoQuery = dbContext.EventGuestPhotos
            .AsNoTracking()
            .Where(photo => dbContext.Events.Any(eventEntity =>
                eventEntity.Id == photo.EventId &&
                eventEntity.OwnerUserId == currentUser.UserId));

        var totalPhotoCount = await ownerPhotoQuery.CountAsync(cancellationToken);
        var totalStorageBytes = await ownerPhotoQuery
            .SumAsync(photo => (long?)photo.FileSizeBytes, cancellationToken) ?? 0;
        var lastUploadAtUtc = await ownerPhotoQuery
            .MaxAsync(photo => (DateTime?)photo.CreatedAtUtc, cancellationToken);

        var largestStorageEvent = await eventAggregates
            .Where(x => x.PhotoCount > 0)
            .OrderByDescending(x => x.TotalStorageBytes)
            .ThenBy(x => x.Event.Id)
            .Select(x => new OwnerStorageHighlight(
                x.Event.Id,
                x.Event.Title,
                x.PhotoCount,
                x.TotalStorageBytes))
            .FirstOrDefaultAsync(cancellationToken);

        var mostPhotosEvent = await eventAggregates
            .Where(x => x.PhotoCount > 0)
            .OrderByDescending(x => x.PhotoCount)
            .ThenByDescending(x => x.TotalStorageBytes)
            .ThenBy(x => x.Event.Id)
            .Select(x => new OwnerStorageHighlight(
                x.Event.Id,
                x.Event.Title,
                x.PhotoCount,
                x.TotalStorageBytes))
            .FirstOrDefaultAsync(cancellationToken);

        var items = await eventAggregates
            .OrderByDescending(x => x.TotalStorageBytes)
            .ThenByDescending(x => x.PhotoCount)
            .ThenByDescending(x => x.Event.EventDateUtc)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new OwnerStorageEventItem(
                x.Event.Id,
                x.Event.Title,
                x.Event.EventDateUtc,
                x.Event.UploadsEnabled,
                x.PhotoCount,
                x.TotalStorageBytes,
                x.PhotoCount == 0 ? 0 : x.TotalStorageBytes / x.PhotoCount,
                x.LastUploadAtUtc))
            .ToListAsync(cancellationToken);

        var totalPages = totalEventCount == 0
            ? 0
            : (int)Math.Ceiling(totalEventCount / (double)request.PageSize);

        var overview = new OwnerStorageOverview(
            totalEventCount,
            totalPhotoCount,
            totalStorageBytes,
            totalEventCount == 0 ? 0 : totalStorageBytes / totalEventCount,
            totalPhotoCount == 0 ? 0 : totalStorageBytes / totalPhotoCount,
            largestStorageEvent,
            mostPhotosEvent,
            lastUploadAtUtc);

        return ApiResult<GetOwnerStorageBreakdownResponse>.Success(
            new GetOwnerStorageBreakdownResponse(
                overview,
                items,
                request.Page,
                request.PageSize,
                totalEventCount,
                totalPages));
    }
}
