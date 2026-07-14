using FluentValidation;

namespace Ortakare.Api.Features.Events.GetMyEvents;

public sealed class GetMyEventsValidator : AbstractValidator<GetMyEventsRequest>
{
    public GetMyEventsValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1);

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100);
    }
}
