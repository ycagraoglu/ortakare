namespace Ortakare.Api.Features.Events.GetEventRecentActivity;

public sealed class GetEventRecentActivityRequest
{
    public int Limit { get; init; } = 10;
}
