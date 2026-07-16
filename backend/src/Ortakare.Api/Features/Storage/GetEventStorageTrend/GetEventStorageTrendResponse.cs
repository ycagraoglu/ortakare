namespace Ortakare.Api.Features.Storage.GetEventStorageTrend;

public sealed record GetEventStorageTrendResponse(
    Guid EventId,
    string Title,
    long TodayAddedBytes,
    int TodayPhotoCount,
    long Last7DaysAddedBytes,
    int Last7DaysPhotoCount,
    long Last30DaysAddedBytes,
    int Last30DaysPhotoCount,
    IReadOnlyList<EventStorageTrendDay> Days);

public sealed record EventStorageTrendDay(
    DateOnly Date,
    long AddedBytes,
    int PhotoCount);
