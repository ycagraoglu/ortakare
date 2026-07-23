namespace Ortakare.Api.Infrastructure.ObjectStorage;

public interface IObjectStorageService
{
    Task UploadAsync(
        string key,
        Stream content,
        string contentType,
        long contentLength,
        CancellationToken cancellationToken);

    Task<Stream> OpenReadAsync(string key, CancellationToken cancellationToken);

    Task<IReadOnlyList<ObjectStorageItem>> ListAsync(
        string prefix,
        int maxKeys,
        CancellationToken cancellationToken);

    Task DeleteAsync(string key, CancellationToken cancellationToken);

    string CreateReadUrl(string key, DateTime expiresAtUtc);
}

public sealed record ObjectStorageItem(
    string Key,
    DateTime LastModifiedUtc,
    long SizeBytes);