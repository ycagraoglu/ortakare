using FluentValidation;

namespace Ortakare.Api.Features.Events.UpdateEvent;

public sealed class UpdateEventValidator : AbstractValidator<UpdateEventRequest>
{
    public UpdateEventValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .MinimumLength(2)
            .MaximumLength(150);

        RuleFor(x => x.EventDateUtc)
            .NotEmpty()
            .Must(value => value.Kind == DateTimeKind.Utc)
            .WithMessage("Etkinlik tarihi UTC formatında gönderilmelidir.");
    }
}