namespace Ortakare.Api.Features.Events.GetMyEvents;

public sealed record GetMyEventsItem(
    Guid Id,
    string Title,
    DateTime EventDateUtc,
    bool UploadsEnabled,
    DateTime CreatedAtUtc);

public sealed record GetMyEventsResponse(
    IReadOnlyList<GetMyEventsItem> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);
