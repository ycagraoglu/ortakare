namespace Ortakare.Api.Features.Storage.GetParticipantStorageBreakdown;

public sealed class GetParticipantStorageBreakdownRequest
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
