using System.Security.Cryptography;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Ortakare.Api.Common;
using Ortakare.Api.Infrastructure.Authentication;
using Ortakare.Api.Infrastructure.Persistence;

namespace Ortakare.Api.Features.Events.RegenerateGalleryToken;

public sealed class RegenerateGalleryTokenHandler(
    OrtakareDbContext dbContext,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
{
    public async Task<ApiResult<RegenerateGalleryTokenResponse>> HandleAsync(
        Guid eventId,
        CancellationToken cancellationToken)
    {
        var eventEntity = await dbContext.Events
            .SingleOrDefaultAsync(
                x => x.Id == eventId && x.OwnerUserId == currentUser.UserId,
                cancellationToken);

        if (eventEntity is null)
        {
            return ApiResult<RegenerateGalleryTokenResponse>.Failure(
                "Etkinlik bulunamadı.",
                StatusCodes.Status404NotFound);
        }

        eventEntity.GalleryToken = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        eventEntity.UpdatedAtUtc = timeProvider.GetUtcNow().UtcDateTime;
        await dbContext.SaveChangesAsync(cancellationToken);

        return ApiResult<RegenerateGalleryTokenResponse>.Success(
            new RegenerateGalleryTokenResponse(
                eventEntity.Id,
                eventEntity.GalleryToken,
                eventEntity.UpdatedAtUtc.Value),
            "Galeri bağlantısı yenilendi.");
    }
}
