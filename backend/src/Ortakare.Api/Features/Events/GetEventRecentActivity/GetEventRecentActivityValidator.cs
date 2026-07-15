using FluentValidation;

namespace Ortakare.Api.Features.Events.GetEventRecentActivity;

public sealed class GetEventRecentActivityValidator : AbstractValidator<GetEventRecentActivityRequest>
{
    public GetEventRecentActivityValidator()
    {
        RuleFor(x => x.Limit)
            .InclusiveBetween(1, 25)
            .WithMessage("Kayıt limiti 1 ile 25 arasında olmalıdır.");
    }
}
