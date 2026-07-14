namespace Ortakare.Api.Features.Participants.GetEventParticipants;

public sealed record GetEventParticipantsResponse(
    IReadOnlyList<GetEventParticipantsItem> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);

public sealed record GetEventParticipantsItem(
    Guid Id,
    string DisplayName,
    int PhotoCount,
    DateTime CreatedAtUtc);
