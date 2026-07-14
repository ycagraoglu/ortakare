namespace Ortakare.Api.Features.Photos.UploadPhoto;

public sealed class PhotoUploadOptions
{
    public const string SectionName = "PhotoUpload";

    public long MaxFileSizeBytes { get; init; } = 25 * 1024 * 1024;
}