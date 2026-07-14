namespace Ortakare.Api.Features.Events.GetMyEvents;

public sealed class GetMyEventsRequest
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
