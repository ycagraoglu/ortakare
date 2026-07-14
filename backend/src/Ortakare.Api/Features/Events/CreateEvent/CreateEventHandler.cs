using System.Security.Cryptography;
using Microsoft.AspNetCore.WebUtilities;
using Ortakare.Api.Common;
using Ortakare.Api.Infrastructure.Authentication;
using Ortakare.Api.Infrastructure.Persistence;

namespace Ortakare.Api.Features.Events.CreateEvent;

public sealed class CreateEventHandler(
    OrtakareDbContext dbContext,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
{
    public async Task<ApiResult<CreateEventResponse>> HandleAsync(
        CreateEventRequest request,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;

        var eventEntity = new Event
        {
            Id = Guid.CreateVersion7(),
            OwnerUserId = currentUser.UserId,
            Title = request.Title.Trim(),
            EventDateUtc = request.EventDateUtc.ToUniversalTime(),
            GalleryToken = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32)),
            UploadsEnabled = true,
            CreatedAtUtc = now
        };

        dbContext.Events.Add(eventEntity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ApiResult<CreateEventResponse>.Success(
            new CreateEventResponse(
                eventEntity.Id,
                eventEntity.Title,
                eventEntity.EventDateUtc,
                eventEntity.GalleryToken,
                eventEntity.UploadsEnabled,
                eventEntity.CreatedAtUtc),
            "Etkinlik oluşturuldu.",
            StatusCodes.Status201Created);
    }
}
