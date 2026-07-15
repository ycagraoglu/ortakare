using FluentValidation;

namespace Ortakare.Api.Features.Dashboard.GetOwnerRecentActivity;

public sealed class GetOwnerRecentActivityValidator : AbstractValidator<GetOwnerRecentActivityRequest>
{
    public GetOwnerRecentActivityValidator()
    {
        RuleFor(x => x.Limit)
            .InclusiveBetween(1, 50)
            .WithMessage("Kayıt limiti 1 ile 50 arasında olmalıdır.");
    }
}
