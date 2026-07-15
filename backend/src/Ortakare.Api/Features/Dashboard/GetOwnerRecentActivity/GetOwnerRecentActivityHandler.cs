using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Ortakare.Api.Common;
using Ortakare.Api.Infrastructure.Authentication;
using Ortakare.Api.Infrastructure.ObjectStorage;
using Ortakare.Api.Infrastructure.Persistence;

namespace Ortakare.Api.Features.Dashboard.GetOwnerRecentActivity;

public sealed class GetOwnerRecentActivityHandler(
    OrtakareDbContext dbContext,
    ICurrentUser currentUser,
    IObjectStorageService objectStorageService,
    IOptions<ObjectStorageOptions> objectStorageOptions,
    TimeProvider timeProvider)
{
    public async Task<ApiResult<GetOwnerRecentActivityResponse>> HandleAsync(
        GetOwnerRecentActivityRequest request,
        CancellationToken cancellationToken)
    {
        var participantRows = await (
            from participant in dbContext.EventGuestParticipants.AsNoTracking()
            join eventEntity in dbContext.Events.AsNoTracking()
                on participant.EventId equals eventEntity.Id
            where eventEntity.OwnerUserId == currentUser.UserId
            orderby participant.CreatedAtUtc descending
            select new ActivityRow(
                "ParticipantJoined",
                eventEntity.Id,
                eventEntity.Title,
                participant.Id,
                participant.DisplayName,
                null,
                null,
                null,
                null,
                participant.IsBlocked,
                participant.CreatedAtUtc))
            .Take(request.Limit)
            .ToListAsync(cancellationToken);

        var photoRows = await (
            from photo in dbContext.EventGuestPhotos.AsNoTracking()
            join participant in dbContext.EventGuestParticipants.AsNoTracking()
                on photo.ParticipantId equals participant.Id
            join eventEntity in dbContext.Events.AsNoTracking()
                on photo.EventId equals eventEntity.Id
            where eventEntity.OwnerUserId == currentUser.UserId
            orderby photo.CreatedAtUtc descending
            select new ActivityRow(
                "PhotoUploaded",
                eventEntity.Id,
                eventEntity.Title,
                participant.Id,
                participant.DisplayName,
                photo.Id,
                photo.ContentType,
                photo.FileSizeBytes,
                photo.StorageKey,
                null,
                photo.CreatedAtUtc))
            .Take(request.Limit)
            .ToListAsync(cancellationToken);

        var readUrlExpiresAtUtc = timeProvider.GetUtcNow()
            .AddMinutes(objectStorageOptions.Value.SignedUrlMinutes)
            .UtcDateTime;

        var items = participantRows
            .Concat(photoRows)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(request.Limit)
            .Select(x => new OwnerRecentActivityItem(
                x.ActivityType,
                x.EventId,
                x.EventTitle,
                x.ParticipantId,
                x.ParticipantDisplayName,
                x.PhotoId,
                x.ContentType,
                x.FileSizeBytes,
                x.StorageKey is null
                    ? null
                    : objectStorageService.CreateReadUrl(x.StorageKey, readUrlExpiresAtUtc),
                x.StorageKey is null ? null : readUrlExpiresAtUtc,
                x.IsBlocked,
                x.CreatedAtUtc))
            .ToList();

        return ApiResult<GetOwnerRecentActivityResponse>.Success(
            new GetOwnerRecentActivityResponse(items));
    }

    private sealed record ActivityRow(
        string ActivityType,
        Guid EventId,
        string EventTitle,
        Guid ParticipantId,
        string ParticipantDisplayName,
        Guid? PhotoId,
        string? ContentType,
        long? FileSizeBytes,
        string? StorageKey,
        bool? IsBlocked,
        DateTime CreatedAtUtc);
}
