namespace Ortakare.Api.Infrastructure.Authentication;

public interface ICurrentUser
{
    Guid UserId { get; }
}
