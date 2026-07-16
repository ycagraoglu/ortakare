using Microsoft.EntityFrameworkCore;
using Ortakare.Api.Common;
using Ortakare.Api.Infrastructure.Authentication;
using Ortakare.Api.Infrastructure.Persistence;

namespace Ortakare.Api.Features.Storage.GetParticipantStorageBreakdown;

public sealed class GetParticipantStorageBreakdownHandler(
    OrtakareDbContext dbContext,
    ICurrentUser currentUser)
{
    public async Task<ApiResult<GetParticipantStorageBreakdownResponse>> HandleAsync(
        Guid eventId,
        GetParticipantStorageBreakdownRequest request,
        CancellationToken cancellationToken)
    {
        var eventInfo = await dbContext.Events
            .AsNoTracking()
            .Where(x => x.Id == eventId && x.OwnerUserId == currentUser.UserId)
            .Select(x => new { x.Id, x.Title })
            .SingleOrDefaultAsync(cancellationToken);

        if (eventInfo is null)
        {
            return ApiResult<GetParticipantStorageBreakdownResponse>.Failure(
                "Etkinlik bulunamadı.",
                StatusCodes.Status404NotFound);
        }

        var participantQuery = dbContext.EventGuestParticipants
            .AsNoTracking()
            .Where(x => x.EventId == eventId);

        var totalCount = await participantQuery.CountAsync(cancellationToken);

        var aggregate = await dbContext.EventGuestPhotos
            .AsNoTracking()
            .Where(x => x.EventId == eventId)
            .GroupBy(_ => 1)
            .Select(group => new
            {
                TotalStorageBytes = group.Sum(x => x.FileSizeBytes),
                TotalPhotoCount = group.Count()
            })
            .SingleOrDefaultAsync(cancellationToken);

        var rows = await participantQuery
            .Select(participant => new
            {
                Participant = participant,
                PhotoCount = dbContext.EventGuestPhotos.Count(photo =>
                    photo.EventId == eventId && photo.ParticipantId == participant.Id),
                TotalStorageBytes = dbContext.EventGuestPhotos
                    .Where(photo => photo.EventId == eventId && photo.ParticipantId == participant.Id)
                    .Sum(photo => (long?)photo.FileSizeBytes) ?? 0,
                LastUploadAtUtc = dbContext.EventGuestPhotos
                    .Where(photo => photo.EventId == eventId && photo.ParticipantId == participant.Id)
                    .Max(photo => (DateTime?)photo.CreatedAtUtc)
            })
            .OrderByDescending(x => x.TotalStorageBytes)
            .ThenByDescending(x => x.PhotoCount)
            .ThenByDescending(x => x.Participant.CreatedAtUtc)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var items = rows
            .Select(x => new ParticipantStorageBreakdownItem(
                x.Participant.Id,
                x.Participant.DisplayName,
                x.Participant.IsBlocked,
                x.Participant.BlockedAtUtc,
                x.Participant.CreatedAtUtc,
                x.PhotoCount,
                x.TotalStorageBytes,
                x.PhotoCount == 0 ? 0 : x.TotalStorageBytes / x.PhotoCount,
                x.LastUploadAtUtc))
            .ToList();

        var totalPages = totalCount == 0
            ? 0
            : (int)Math.Ceiling(totalCount / (double)request.PageSize);

        return ApiResult<GetParticipantStorageBreakdownResponse>.Success(
            new GetParticipantStorageBreakdownResponse(
                eventInfo.Id,
                eventInfo.Title,
                aggregate?.TotalStorageBytes ?? 0,
                aggregate?.TotalPhotoCount ?? 0,
                items,
                request.Page,
                request.PageSize,
                totalCount,
                totalPages));
    }
}
