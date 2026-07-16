using Microsoft.EntityFrameworkCore;
using Ortakare.Api.Common;
using Ortakare.Api.Infrastructure.Authentication;
using Ortakare.Api.Infrastructure.Persistence;

namespace Ortakare.Api.Features.EventAudit.GetEventAuditLogs;

public sealed class GetEventAuditLogsHandler(
    OrtakareDbContext dbContext,
    ICurrentUser currentUser)
{
    public async Task<ApiResult<GetEventAuditLogsResponse>> HandleAsync(
        Guid eventId,
        GetEventAuditLogsRequest request,
        CancellationToken cancellationToken)
    {
        var ownsEvent = await dbContext.Events.AsNoTracking()
            .AnyAsync(x => x.Id == eventId && x.OwnerUserId == currentUser.UserId, cancellationToken);

        if (!ownsEvent)
        {
            return ApiResult<GetEventAuditLogsResponse>.Failure(
                "Etkinlik bulunamadı.",
                StatusCodes.Status404NotFound);
        }

        var query = dbContext.EventAuditLogs.AsNoTracking()
            .Where(x => x.EventId == eventId && x.OwnerUserId == currentUser.UserId);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .ThenByDescending(x => x.Id)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new EventAuditLogItem(
                x.Id,
                x.Action,
                x.ActorType,
                x.ActorId,
                x.TargetType,
                x.TargetId,
                x.Description,
                x.MetadataJson,
                x.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        var totalPages = totalCount == 0
            ? 0
            : (int)Math.Ceiling(totalCount / (double)request.PageSize);

        return ApiResult<GetEventAuditLogsResponse>.Success(
            new GetEventAuditLogsResponse(items, request.Page, request.PageSize, totalCount, totalPages));
    }
}
