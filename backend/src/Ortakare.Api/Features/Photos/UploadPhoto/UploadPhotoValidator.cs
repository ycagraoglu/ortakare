using FluentValidation;
using Microsoft.Extensions.Options;

namespace Ortakare.Api.Features.Photos.UploadPhoto;

public sealed class UploadPhotoValidator : AbstractValidator<UploadPhotoRequest>
{
    public UploadPhotoValidator(IOptions<PhotoUploadOptions> options)
    {
        var maxFileSizeBytes = options.Value.MaxFileSizeBytes;

        RuleFor(x => x.File)
            .NotNull()
            .WithMessage("Fotoğraf dosyası zorunludur.");

        When(x => x.File is not null, () =>
        {
            RuleFor(x => x.File!.Length)
                .GreaterThan(0)
                .WithMessage("Fotoğraf dosyası boş olamaz.")
                .LessThanOrEqualTo(maxFileSizeBytes)
                .WithMessage($"Fotoğraf boyutu en fazla {maxFileSizeBytes / 1024 / 1024} MB olabilir.");
        });
    }
}