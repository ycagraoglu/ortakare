using Microsoft.EntityFrameworkCore;
using Ortakare.Api.Common;
using Ortakare.Api.Infrastructure.Authentication;
using Ortakare.Api.Infrastructure.Persistence;

namespace Ortakare.Api.Features.Storage.GetStorageUsageTrend;

public sealed class GetStorageUsageTrendHandler(
    OrtakareDbContext dbContext,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
{
    private const int TrendDayCount = 30;

    public async Task<ApiResult<GetStorageUsageTrendResponse>> HandleAsync(
        CancellationToken cancellationToken)
    {
        var todayUtc = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var firstDay = todayUtc.AddDays(-(TrendDayCount - 1));
        var firstDayUtc = firstDay.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        var groupedRows = await dbContext.EventGuestPhotos
            .AsNoTracking()
            .Where(photo =>
                photo.CreatedAtUtc >= firstDayUtc &&
                dbContext.Events.Any(@event =>
                    @event.Id == photo.EventId &&
                    @event.OwnerUserId == currentUser.UserId))
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
                return new StorageUsageTrendDay(date, values.AddedBytes, values.PhotoCount);
            })
            .ToList();

        var last7DaysStart = todayUtc.AddDays(-6);
        var today = days.Single(x => x.Date == todayUtc);
        var last7Days = days.Where(x => x.Date >= last7DaysStart).ToList();

        return ApiResult<GetStorageUsageTrendResponse>.Success(
            new GetStorageUsageTrendResponse(
                today.AddedBytes,
                today.PhotoCount,
                last7Days.Sum(x => x.AddedBytes),
                last7Days.Sum(x => x.PhotoCount),
                days.Sum(x => x.AddedBytes),
                days.Sum(x => x.PhotoCount),
                days));
    }
}
