using FluentValidation;

namespace Ortakare.Api.Features.Storage.ValidateUpload;

public sealed record ValidateUploadRequest(
    Guid EventId,
    int FileCount,
    long TotalBytes,
    long LargestFileBytes);

public sealed class ValidateUploadRequestValidator : AbstractValidator<ValidateUploadRequest>
{
    public ValidateUploadRequestValidator()
    {
        RuleFor(x => x.EventId).NotEmpty().WithMessage("Etkinlik kimliği zorunludur.");
        RuleFor(x => x.FileCount).InclusiveBetween(1, 100).WithMessage("Dosya sayısı 1 ile 100 arasında olmalıdır.");
        RuleFor(x => x.TotalBytes).GreaterThan(0).WithMessage("Toplam dosya boyutu sıfırdan büyük olmalıdır.");
        RuleFor(x => x.LargestFileBytes).GreaterThan(0).WithMessage("En büyük dosya boyutu sıfırdan büyük olmalıdır.");
        RuleFor(x => x).Must(x => x.LargestFileBytes <= x.TotalBytes)
            .WithMessage("En büyük dosya boyutu toplam boyuttan büyük olamaz.");
    }
}