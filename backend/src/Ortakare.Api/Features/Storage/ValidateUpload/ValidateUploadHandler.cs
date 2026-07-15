using Microsoft.EntityFrameworkCore;
using Ortakare.Api.Common;
using Ortakare.Api.Infrastructure.Authentication;
using Ortakare.Api.Infrastructure.Persistence;

namespace Ortakare.Api.Features.Storage.ValidateUpload;

public sealed class ValidateUploadHandler(
    OrtakareDbContext dbContext,
    ICurrentUser currentUser,
    StorageUploadPolicyService storageUploadPolicyService)
{
    public async Task<ApiResult<ValidateUploadResponse>> HandleAsync(
        ValidateUploadRequest request,
        CancellationToken cancellationToken)
    {
        var eventInfo = await dbContext.Events
            .AsNoTracking()
            .Where(x => x.Id == request.EventId && x.OwnerUserId == currentUser.UserId)
            .Select(x => new { x.UploadsEnabled, x.OwnerUserId })
            .SingleOrDefaultAsync(cancellationToken);

        if (eventInfo is null)
        {
            return ApiResult<ValidateUploadResponse>.Failure(
                "Etkinlik bulunamadı.",
                StatusCodes.Status404NotFound);
        }

        var decision = await storageUploadPolicyService.EvaluateAsync(
            eventInfo.OwnerUserId,
            eventInfo.UploadsEnabled,
            request.FileCount,
            request.TotalBytes,
            request.LargestFileBytes,
            cancellationToken);

        return ApiResult<ValidateUploadResponse>.Success(
            new ValidateUploadResponse(
                decision.CanUpload,
                decision.QuotaStatus,
                decision.QuotaBytes,
                decision.CurrentUsageBytes,
                decision.RemainingBytes,
                decision.RequestedBytes,
                decision.ProjectedUsageBytes,
                decision.ProjectedUsagePercent,
                decision.MaxFileSizeBytes,
                decision.MaxFilesPerRequest,
                decision.Message));
    }
}