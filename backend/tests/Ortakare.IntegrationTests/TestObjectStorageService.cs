using System.Collections.Concurrent;
using Ortakare.Api.Infrastructure.ObjectStorage;

namespace Ortakare.IntegrationTests;

public sealed class TestObjectStorageService : IObjectStorageService
{
    private readonly ConcurrentDictionary<string, StoredObject> _objects = new();

    public int UploadCount { get; private set; }
    public int DeleteCount { get; private set; }
    public bool ThrowOnDelete { get; set; }
    public bool ThrowOnRead { get; set; }

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

    public Task<Stream> OpenReadAsync(string key, CancellationToken cancellationToken)
    {
        if (ThrowOnRead)
        {
            throw new InvalidOperationException("Simulated object storage read failure.");
        }

        if (!_objects.TryGetValue(key, out var storedObject))
        {
            throw new FileNotFoundException("Stored object was not found.", key);
        }

        Stream stream = new MemoryStream(storedObject.Content, writable: false);
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string key, CancellationToken cancellationToken)
    {
        if (ThrowOnDelete)
        {
            throw new InvalidOperationException("Simulated object storage delete failure.");
        }

        _objects.TryRemove(key, out _);
        DeleteCount++;
        return Task.CompletedTask;
    }

    public string CreateReadUrl(string key, DateTime expiresAtUtc) =>
        $"https://storage.test/{Uri.EscapeDataString(key)}?expires={Uri.EscapeDataString(expiresAtUtc.ToString("O"))}";

    public void Reset()
    {
        _objects.Clear();
        UploadCount = 0;
        DeleteCount = 0;
        ThrowOnDelete = false;
        ThrowOnRead = false;
    }
}

public sealed record StoredObject(byte[] Content, string ContentType, long ContentLength);