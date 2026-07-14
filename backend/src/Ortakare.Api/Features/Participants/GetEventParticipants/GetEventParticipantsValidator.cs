using FluentValidation;

namespace Ortakare.Api.Features.Participants.GetEventParticipants;

public sealed class GetEventParticipantsValidator : AbstractValidator<GetEventParticipantsRequest>
{
    public GetEventParticipantsValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThan(0);

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100);
    }
}
