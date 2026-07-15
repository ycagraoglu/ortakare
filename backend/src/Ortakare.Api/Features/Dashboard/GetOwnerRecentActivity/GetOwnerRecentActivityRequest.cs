namespace Ortakare.Api.Features.Dashboard.GetOwnerRecentActivity;

public sealed class GetOwnerRecentActivityRequest
{
    public int Limit { get; init; } = 20;
}
