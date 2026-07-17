using Microsoft.EntityFrameworkCore;
using Ortakare.Api.Common;
using Ortakare.Api.Infrastructure.Authentication;
using Ortakare.Api.Infrastructure.Persistence;

namespace Ortakare.Api.Features.GalleryExports.GetGalleryExport;

public sealed class GetGalleryExportHandler(
    OrtakareDbContext dbContext,
    ICurrentUser currentUser)
{
    public async Task<ApiResult<GetGalleryExportResponse>> HandleAsync(
        Guid eventId,
        Guid exportId,
        CancellationToken cancellationToken)
    {
        var galleryExport = await dbContext.GalleryExports
            .AsNoTracking()
            .Where(x => x.Id == exportId && x.EventId == eventId)
            .Where(x => dbContext.Events.Any(e => e.Id == x.EventId && e.OwnerUserId == currentUser.UserId))
            .Select(x => new GetGalleryExportResponse(
                x.Id,
                x.Status,
                x.PhotoCount,
                x.CreatedAtUtc,
                x.CompletedAtUtc,
                x.FailedAtUtc))
            .SingleOrDefaultAsync(cancellationToken);

        if (galleryExport is null)
        {
            return ApiResult<GetGalleryExportResponse>.Failure(
                "Dışa aktarma kaydı bulunamadı.",
                StatusCodes.Status404NotFound);
        }

        return ApiResult<GetGalleryExportResponse>.Success(galleryExport);
    }
}
