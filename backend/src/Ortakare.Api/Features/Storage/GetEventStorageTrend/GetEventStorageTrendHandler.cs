using Microsoft.EntityFrameworkCore;
using Ortakare.Api.Common;
using Ortakare.Api.Infrastructure.Authentication;
using Ortakare.Api.Infrastructure.Persistence;

namespace Ortakare.Api.Features.Storage.GetEventStorageTrend;

public sealed class GetEventStorageTrendHandler(
    OrtakareDbContext dbContext,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
{
    private const int TrendDayCount = 30;

    public async Task<ApiResult<GetEventStorageTrendResponse>> HandleAsync(
        Guid eventId,
        CancellationToken cancellationToken)
    {
        var eventInfo = await dbContext.Events
            .AsNoTracking()
            .Where(x => x.Id == eventId && x.OwnerUserId == currentUser.UserId)
            .Select(x => new { x.Id, x.Title })
            .SingleOrDefaultAsync(cancellationToken);

        if (eventInfo is null)
        {
            return ApiResult<GetEventStorageTrendResponse>.Failure(
                "Etkinlik bulunamadı.",
                StatusCodes.Status404NotFound);
        }

        var todayUtc = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var firstDay = todayUtc.AddDays(-(TrendDayCount - 1));
        var firstDayUtc = firstDay.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        var groupedRows = await dbContext.EventGuestPhotos
            .AsNoTracking()
            .Where(photo => photo.EventId == eventInfo.Id && photo.CreatedAtUtc >= firstDayUtc)
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
                return new EventStorageTrendDay(date, values.AddedBytes, values.PhotoCount);
            })
            .ToList();

        var last7DaysStart = todayUtc.AddDays(-6);
        var today = days.Single(x => x.Date == todayUtc);
        var last7Days = days.Where(x => x.Date >= last7DaysStart).ToList();

        return ApiResult<GetEventStorageTrendResponse>.Success(
            new GetEventStorageTrendResponse(
                eventInfo.Id,
                eventInfo.Title,
                today.AddedBytes,
                today.PhotoCount,
                last7Days.Sum(x => x.AddedBytes),
                last7Days.Sum(x => x.PhotoCount),
                days.Sum(x => x.AddedBytes),
                days.Sum(x => x.PhotoCount),
                days));
    }
}
