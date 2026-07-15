using FluentValidation;

namespace Ortakare.Api.Features.Auth.Logout;

public sealed class LogoutValidator : AbstractValidator<LogoutRequest>
{
    public LogoutValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty()
            .WithMessage("Refresh token zorunludur.");
    }
}
