using Microsoft.EntityFrameworkCore;
using Ortakare.Api.Common;
using Ortakare.Api.Infrastructure.Authentication;
using Ortakare.Api.Infrastructure.Persistence;

namespace Ortakare.Api.Features.Dashboard.GetOwnerDashboardSummary;

public sealed class GetOwnerDashboardSummaryHandler(
    OrtakareDbContext dbContext,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
{
    private const int UpcomingEventLimit = 5;

    public async Task<ApiResult<GetOwnerDashboardSummaryResponse>> HandleAsync(
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var ownerEvents = dbContext.Events
            .AsNoTracking()
            .Where(x => x.OwnerUserId == currentUser.UserId);

        var totalEventCount = await ownerEvents.CountAsync(cancellationToken);
        var upcomingEventCount = await ownerEvents.CountAsync(
            x => x.EventDateUtc >= now,
            cancellationToken);
        var openEventCount = await ownerEvents.CountAsync(
            x => x.UploadsEnabled,
            cancellationToken);

        var participantCount = await dbContext.EventGuestParticipants
            .AsNoTracking()
            .CountAsync(
                participant => ownerEvents.Any(eventEntity => eventEntity.Id == participant.EventId),
                cancellationToken);

        var blockedParticipantCount = await dbContext.EventGuestParticipants
            .AsNoTracking()
            .CountAsync(
                participant => participant.IsBlocked &&
                    ownerEvents.Any(eventEntity => eventEntity.Id == participant.EventId),
                cancellationToken);

        var photoAggregate = await dbContext.EventGuestPhotos
            .AsNoTracking()
            .Where(photo => ownerEvents.Any(eventEntity => eventEntity.Id == photo.EventId))
            .GroupBy(_ => 1)
            .Select(group => new
            {
                Count = group.Count(),
                TotalSizeBytes = group.Sum(photo => photo.FileSizeBytes)
            })
            .SingleOrDefaultAsync(cancellationToken);

        var upcomingEvents = await ownerEvents
            .Where(x => x.EventDateUtc >= now)
            .OrderBy(x => x.EventDateUtc)
            .ThenBy(x => x.Id)
            .Take(UpcomingEventLimit)
            .Select(eventEntity => new UpcomingEventItem(
                eventEntity.Id,
                eventEntity.Title,
                eventEntity.EventDateUtc,
                eventEntity.UploadsEnabled,
                dbContext.EventGuestParticipants.Count(participant => participant.EventId == eventEntity.Id),
                dbContext.EventGuestPhotos.Count(photo => photo.EventId == eventEntity.Id)))
            .ToListAsync(cancellationToken);

        return ApiResult<GetOwnerDashboardSummaryResponse>.Success(
            new GetOwnerDashboardSummaryResponse(
                totalEventCount,
                upcomingEventCount,
                totalEventCount - upcomingEventCount,
                openEventCount,
                totalEventCount - openEventCount,
                participantCount,
                blockedParticipantCount,
                photoAggregate?.Count ?? 0,
                photoAggregate?.TotalSizeBytes ?? 0,
                upcomingEvents));
    }
}
