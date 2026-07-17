using Ortakare.Api.Common;
using Ortakare.Api.Features.Notifications.Streaming;
using Ortakare.Api.Infrastructure.Authentication;

namespace Ortakare.Api.Features.Notifications.CreateNotificationStreamToken;

public sealed class CreateNotificationStreamTokenHandler(
    INotificationStreamTokenService tokenService,
    ICurrentUser currentUser)
{
    public ApiResult<CreateNotificationStreamTokenResponse> Handle()
    {
        var issuedToken = tokenService.Issue(currentUser.UserId);

        return ApiResult<CreateNotificationStreamTokenResponse>.Success(
            new CreateNotificationStreamTokenResponse(
                issuedToken.Token,
                issuedToken.ExpiresAtUtc));
    }
}
