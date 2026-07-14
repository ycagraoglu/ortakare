using System.Collections.Concurrent;
using Ortakare.Api.Infrastructure.ObjectStorage;

namespace Ortakare.IntegrationTests;

public sealed class TestObjectStorageService : IObjectStorageService
{
    private readonly ConcurrentDictionary<string, StoredObject> _objects = new();

    public int UploadCount { get; private set; }

    public IReadOnlyDictionary<string, StoredObject> Objects => _objects;

    public async Task UploadAsync(
        string key,
        Stream content,
        string contentType,
        long contentLength,
        CancellationToken cancellationToken)
    {
        using var memoryStream = new MemoryStream();
        await content.CopyToAsync(memoryStream, cancellationToken);

        _objects[key] = new StoredObject(
            memoryStream.ToArray(),
            contentType,
            contentLength);

        UploadCount++;
    }

    public Task DeleteAsync(string key, CancellationToken cancellationToken)
    {
        _objects.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    public string CreateReadUrl(string key, DateTime expiresAtUtc) =>
        $"https://storage.test/{Uri.EscapeDataString(key)}?expires={Uri.EscapeDataString(expiresAtUtc.ToString("O"))}";

    public void Reset()
    {
        _objects.Clear();
        UploadCount = 0;
    }
}

public sealed record StoredObject(byte[] Content, string ContentType, long ContentLength);