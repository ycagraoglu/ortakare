using Microsoft.EntityFrameworkCore;
using Ortakare.Api.Common;
using Ortakare.Api.Infrastructure.Authentication;
using Ortakare.Api.Infrastructure.Persistence;

namespace Ortakare.Api.Features.Events.GetEvent;

public sealed class GetEventHandler(
    OrtakareDbContext dbContext,
    ICurrentUser currentUser)
{
    public async Task<ApiResult<GetEventResponse>> HandleAsync(
        Guid eventId,
        CancellationToken cancellationToken)
    {
        var eventResponse = await dbContext.Events
            .AsNoTracking()
            .Where(x => x.Id == eventId && x.OwnerUserId == currentUser.UserId)
            .Select(x => new GetEventResponse(
                x.Id,
                x.Title,
                x.EventDateUtc,
                x.GalleryToken,
                x.UploadsEnabled,
                x.CreatedAtUtc))
            .SingleOrDefaultAsync(cancellationToken);

        return eventResponse is null
            ? ApiResult<GetEventResponse>.Failure("Event not found.", StatusCodes.Status404NotFound)
            : ApiResult<GetEventResponse>.Success(eventResponse);
    }
}
