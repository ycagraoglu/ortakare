namespace Ortakare.Api.Features.Photos.UploadPhoto;

public sealed class PhotoUploadOptions
{
    public const string SectionName = "PhotoUpload";

    public long MaxFileSizeBytes { get; init; } = 25 * 1024 * 1024;
    public int MaxWidth { get; init; } = 12000;
    public int MaxHeight { get; init; } = 12000;
    public long MaxPixelCount { get; init; } = 100_000_000;
    public int MaxOriginalFileNameLength { get; init; } = 255;
}