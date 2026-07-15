namespace Ortakare.Api.Features.Events.RegenerateGalleryToken;

public sealed record RegenerateGalleryTokenResponse(
    Guid Id,
    string GalleryToken,
    DateTime UpdatedAtUtc);
