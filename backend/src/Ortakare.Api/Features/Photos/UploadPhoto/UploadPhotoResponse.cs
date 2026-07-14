namespace Ortakare.Api.Features.Photos.UploadPhoto;

public sealed record UploadPhotoResponse(
    Guid PhotoId,
    Guid ClientUploadId,
    string ContentType,
    long FileSizeBytes,
    DateTime CreatedAtUtc,
    bool AlreadyUploaded);