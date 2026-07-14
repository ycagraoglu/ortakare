using Microsoft.EntityFrameworkCore;
using Ortakare.Api.Common;
using Ortakare.Api.Infrastructure.Authentication;
using Ortakare.Api.Infrastructure.Persistence;

namespace Ortakare.Api.Features.Events.GetMyEvents;

public sealed class GetMyEventsHandler(
    OrtakareDbContext dbContext,
    ICurrentUser currentUser)
{
    public async Task<ApiResult<GetMyEventsResponse>> HandleAsync(
        GetMyEventsRequest request,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Events
            .AsNoTracking()
            .Where(x => x.OwnerUserId == currentUser.UserId);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(x => x.EventDateUtc)
            .ThenByDescending(x => x.CreatedAtUtc)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new GetMyEventsItem(
                x.Id,
                x.Title,
                x.EventDateUtc,
                x.UploadsEnabled,
                x.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        var totalPages = totalCount == 0
            ? 0
            : (int)Math.Ceiling(totalCount / (double)request.PageSize);

        return ApiResult<GetMyEventsResponse>.Success(
            new GetMyEventsResponse(
                items,
                request.Page,
                request.PageSize,
                totalCount,
                totalPages));
    }
}
