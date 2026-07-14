using FluentValidation;

namespace Ortakare.Api.Features.Participants.UpdateParticipantDisplayName;

public sealed class UpdateParticipantDisplayNameValidator
    : AbstractValidator<UpdateParticipantDisplayNameRequest>
{
    public UpdateParticipantDisplayNameValidator()
    {
        RuleFor(x => x.DisplayName)
            .NotEmpty()
            .MaximumLength(80);
    }
}
