namespace Ortakare.Api.Features.Dashboard.GetOwnerDashboardSummary;

public sealed record GetOwnerDashboardSummaryResponse(
    int TotalEventCount,
    int UpcomingEventCount,
    int PastEventCount,
    int OpenEventCount,
    int ClosedEventCount,
    int ParticipantCount,
    int BlockedParticipantCount,
    int PhotoCount,
    long TotalPhotoSizeBytes,
    IReadOnlyList<UpcomingEventItem> UpcomingEvents);

public sealed record UpcomingEventItem(
    Guid EventId,
    string Title,
    DateTime EventDateUtc,
    bool UploadsEnabled,
    int ParticipantCount,
    int PhotoCount);
