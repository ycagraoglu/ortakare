using FluentValidation;

namespace Ortakare.Api.Features.Events.CreateEvent;

public sealed class CreateEventValidator : AbstractValidator<CreateEventRequest>
{
    public CreateEventValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .MinimumLength(2)
            .MaximumLength(150);

        RuleFor(x => x.EventDateUtc)
            .NotEmpty()
            .Must(x => x.Kind == DateTimeKind.Utc)
            .WithMessage("Etkinlik tarihi UTC formatında gönderilmelidir.");
    }
}
