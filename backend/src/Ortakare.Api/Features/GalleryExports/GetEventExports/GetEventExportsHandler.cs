using Microsoft.EntityFrameworkCore;
using Ortakare.Api.Common;
using Ortakare.Api.Infrastructure.Authentication;
using Ortakare.Api.Infrastructure.Persistence;

namespace Ortakare.Api.Features.GalleryExports.GetEventExports;

public sealed class GetEventExportsHandler(
    OrtakareDbContext dbContext,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
{
    public async Task<ApiResult<GetEventExportsResponse>> HandleAsync(
        Guid eventId,
        GetEventExportsRequest request,
        CancellationToken cancellationToken)
    {
        var ownsEvent = await dbContext.Events
            .AsNoTracking()
            .AnyAsync(
                x => x.Id == eventId && x.OwnerUserId == currentUser.UserId,
                cancellationToken);

        if (!ownsEvent)
        {
            return ApiResult<GetEventExportsResponse>.Failure(
                "Etkinlik bulunamadı.",
                StatusCodes.Status404NotFound);
        }

        var query = dbContext.GalleryExports
            .AsNoTracking()
            .Where(x => x.EventId == eventId);

        var totalCount = await query.CountAsync(cancellationToken);
        var now = timeProvider.GetUtcNow().UtcDateTime;

        var items = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .ThenByDescending(x => x.Id)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new GetEventExportsItem(
                x.Id,
                x.Status,
                x.PhotoCount,
                x.CreatedAtUtc,
                x.CompletedAtUtc,
                x.ExpiresAtUtc,
                x.Status == GalleryExportStatus.Completed &&
                x.ExpiresAtUtc.HasValue &&
                x.ExpiresAtUtc.Value <= now,
                x.FailedAtUtc))
            .ToListAsync(cancellationToken);

        var totalPages = totalCount == 0
            ? 0
            : (int)Math.Ceiling(totalCount / (double)request.PageSize);

        return ApiResult<GetEventExportsResponse>.Success(
            new GetEventExportsResponse(
                items,
                request.Page,
                request.PageSize,
                totalCount,
                totalPages));
    }
}
