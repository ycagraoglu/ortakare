namespace Ortakare.Api.Features.Storage.GetStorageUsageTrend;

public sealed record GetStorageUsageTrendResponse(
    long TodayAddedBytes,
    int TodayPhotoCount,
    long Last7DaysAddedBytes,
    int Last7DaysPhotoCount,
    long Last30DaysAddedBytes,
    int Last30DaysPhotoCount,
    IReadOnlyList<StorageUsageTrendDay> Days);

public sealed record StorageUsageTrendDay(
    DateOnly Date,
    long AddedBytes,
    int PhotoCount);
