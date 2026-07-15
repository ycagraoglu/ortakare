using FluentValidation;

namespace Ortakare.Api.Features.Dashboard.GetOwnerStorageBreakdown;

public sealed class GetOwnerStorageBreakdownValidator : AbstractValidator<GetOwnerStorageBreakdownRequest>
{
    public GetOwnerStorageBreakdownValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Sayfa numarası en az 1 olmalıdır.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("Sayfa boyutu 1 ile 100 arasında olmalıdır.");
    }
}
