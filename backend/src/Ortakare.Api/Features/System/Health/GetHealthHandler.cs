using Ortakare.Api.Common;

namespace Ortakare.Api.Features.System.Health;

public sealed class GetHealthHandler(TimeProvider timeProvider)
{
    public Task<ApiResult<HealthResponse>> HandleAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var response = new HealthResponse(
            "Ortakare.Api",
            "Healthy",
            timeProvider.GetUtcNow().UtcDateTime);

        return Task.FromResult(ApiResult<HealthResponse>.Success(response));
    }
}
