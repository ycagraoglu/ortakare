using Ortakare.Api.Features.Users;

namespace Ortakare.Api.Infrastructure.Authentication;

public interface IAccessTokenService
{
    AccessToken Create(User user);
}
