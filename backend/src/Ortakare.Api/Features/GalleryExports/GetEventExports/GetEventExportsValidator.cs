using FluentValidation;

namespace Ortakare.Api.Features.GalleryExports.GetEventExports;

public sealed class GetEventExportsValidator : AbstractValidator<GetEventExportsRequest>
{
    public GetEventExportsValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThan(0)
            .WithMessage("Sayfa numarası 1 veya daha büyük olmalıdır.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("Sayfa boyutu 1 ile 100 arasında olmalıdır.");
    }
}