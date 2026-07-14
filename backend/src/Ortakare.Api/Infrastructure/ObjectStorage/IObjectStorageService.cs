namespace Ortakare.Api.Infrastructure.ObjectStorage;

public interface IObjectStorageService
{
    Task UploadAsync(
        string key,
        Stream content,
        string contentType,
        long contentLength,
        CancellationToken cancellationToken);

    Task DeleteAsync(string key, CancellationToken cancellationToken);
}