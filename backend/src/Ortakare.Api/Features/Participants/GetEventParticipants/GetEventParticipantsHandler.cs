using Microsoft.EntityFrameworkCore;
using Ortakare.Api.Common;
using Ortakare.Api.Infrastructure.Authentication;
using Ortakare.Api.Infrastructure.Persistence;

namespace Ortakare.Api.Features.Participants.GetEventParticipants;

public sealed class GetEventParticipantsHandler(
    OrtakareDbContext dbContext,
    ICurrentUser currentUser)
{
    public async Task<ApiResult<GetEventParticipantsResponse>> HandleAsync(
        Guid eventId,
        GetEventParticipantsRequest request,
        CancellationToken cancellationToken)
    {
        var ownsEvent = await dbContext.Events
            .AsNoTracking()
            .AnyAsync(
                x => x.Id == eventId && x.OwnerUserId == currentUser.UserId,
                cancellationToken);

        if (!ownsEvent)
        {
            return ApiResult<GetEventParticipantsResponse>.Failure(
                "Etkinlik bulunamadı.",
                StatusCodes.Status404NotFound);
        }

        var query = dbContext.EventGuestParticipants
            .AsNoTracking()
            .Where(x => x.EventId == eventId);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(participant => new GetEventParticipantsItem(
                participant.Id,
                participant.DisplayName,
                dbContext.EventGuestPhotos.Count(photo => photo.ParticipantId == participant.Id),
                participant.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        var totalPages = totalCount == 0
            ? 0
            : (int)Math.Ceiling(totalCount / (double)request.PageSize);

        return ApiResult<GetEventParticipantsResponse>.Success(
            new GetEventParticipantsResponse(
                items,
                request.Page,
                request.PageSize,
                totalCount,
                totalPages));
    }
}
