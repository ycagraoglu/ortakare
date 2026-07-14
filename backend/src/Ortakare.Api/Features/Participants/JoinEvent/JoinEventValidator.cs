using FluentValidation;

namespace Ortakare.Api.Features.Participants.JoinEvent;

public sealed class JoinEventValidator : AbstractValidator<JoinEventRequest>
{
    public JoinEventValidator()
    {
        RuleFor(x => x.DisplayName)
            .NotEmpty()
            .MinimumLength(2)
            .MaximumLength(80);
    }
}
