using Microsoft.EntityFrameworkCore;
using Ortakare.Api.Common;
using Ortakare.Api.Infrastructure.Persistence;

namespace Ortakare.Api.Features.PublicEvents.GetPublicEvent;

public sealed class GetPublicEventHandler(OrtakareDbContext dbContext)
{
    public async Task<ApiResult<GetPublicEventResponse>> HandleAsync(
        string galleryToken,
        CancellationToken cancellationToken)
    {
        var eventInfo = await dbContext.Events
            .AsNoTracking()
            .Where(x => x.GalleryToken == galleryToken)
            .Select(x => new GetPublicEventResponse(
                x.Title,
                x.EventDateUtc,
                x.UploadsEnabled))
            .SingleOrDefaultAsync(cancellationToken);

        if (eventInfo is null)
        {
            return ApiResult<GetPublicEventResponse>.Failure(
                "Etkinlik bulunamadı.",
                StatusCodes.Status404NotFound);
        }

        return ApiResult<GetPublicEventResponse>.Success(eventInfo);
    }
}
