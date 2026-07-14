namespace Ortakare.Api.Features.Photos.UploadPhoto;

public sealed class UploadPhotoRequest
{
    public IFormFile? File { get; init; }
}