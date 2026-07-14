using FluentValidation;

namespace Ortakare.Api.Features.Photos.GetEventPhotos;

public sealed class GetEventPhotosValidator : AbstractValidator<GetEventPhotosRequest>
{
    public GetEventPhotosValidator()
    {
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}