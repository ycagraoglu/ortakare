using Ortakare.Api.Features.GalleryExports;

namespace Ortakare.Api.Features.Events.GetEventSummary;

public sealed record GetEventSummaryResponse(
    Guid EventId,
    string Title,
    DateTime EventDateUtc,
    bool UploadsEnabled,
    int ParticipantCount,
    int BlockedParticipantCount,
    int PhotoCount,
    long TotalPhotoSizeBytes,
    GalleryExportSummary ExportSummary);

public sealed record GalleryExportSummary(
    int TotalCount,
    int PendingCount,
    int ProcessingCount,
    int CompletedCount,
    int FailedCount,
    int CancelledCount);
