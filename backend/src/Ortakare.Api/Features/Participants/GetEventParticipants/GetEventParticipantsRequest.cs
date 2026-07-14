namespace Ortakare.Api.Features.Participants.GetEventParticipants;

public sealed class GetEventParticipantsRequest
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 30;
}
