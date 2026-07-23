using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;

namespace Ortakare.Api.Infrastructure.ObjectStorage;

public sealed class R2ObjectStorageService(
    IAmazonS3 s3Client,
    IOptions<ObjectStorageOptions> options) : IObjectStorageService
{
    private readonly ObjectStorageOptions _options = options.Value;

    public async Task UploadAsync(
        string key,
        Stream content,
        string contentType,
        long contentLength,
        CancellationToken cancellationToken)
    {
        var request = new PutObjectRequest
        {
            BucketName = _options.BucketName,
            Key = key,
            InputStream = content,
            ContentType = contentType,
            AutoCloseStream = false,
            DisablePayloadSigning = true
        };

        request.Headers.ContentLength = contentLength;
        await s3Client.PutObjectAsync(request, cancellationToken);
    }

    public async Task<Stream> OpenReadAsync(
        string key,
        CancellationToken cancellationToken)
    {
        var response = await s3Client.GetObjectAsync(
            new GetObjectRequest
            {
                BucketName = _options.BucketName,
                Key = key
            },
            cancellationToken);

        return new S3ResponseStream(response);
    }

    public async Task<IReadOnlyList<ObjectStorageItem>> ListAsync(
        string prefix,
        int maxKeys,
        CancellationToken cancellationToken)
    {
        var result = new List<ObjectStorageItem>(maxKeys);
        string? continuationToken = null;

        do
        {
            var response = await s3Client.ListObjectsV2Async(
                new ListObjectsV2Request
                {
                    BucketName = _options.BucketName,
                    Prefix = prefix,
                    MaxKeys = Math.Min(1000, maxKeys - result.Count),
                    ContinuationToken = continuationToken
                },
                cancellationToken);

            result.AddRange(response.S3Objects.Select(x => new ObjectStorageItem(
                x.Key,
                x.LastModified.ToUniversalTime(),
                x.Size)));

            continuationToken = response.IsTruncated && result.Count < maxKeys
                ? response.NextContinuationToken
                : null;
        }
        while (!string.IsNullOrWhiteSpace(continuationToken));

        return result;
    }

    public Task DeleteAsync(string key, CancellationToken cancellationToken) =>
        s3Client.DeleteObjectAsync(
            new DeleteObjectRequest
            {
                BucketName = _options.BucketName,
                Key = key
            },
            cancellationToken);

    public string CreateReadUrl(string key, DateTime expiresAtUtc) =>
        s3Client.GetPreSignedURL(new GetPreSignedUrlRequest
        {
            BucketName = _options.BucketName,
            Key = key,
            Verb = HttpVerb.GET,
            Expires = expiresAtUtc
        });

    private sealed class S3ResponseStream(GetObjectResponse response) : Stream
    {
        private readonly Stream _inner = response.ResponseStream;

        public override bool CanRead => _inner.CanRead;
        public override bool CanSeek => _inner.CanSeek;
        public override bool CanWrite => false;
        public override long Length => _inner.Length;
        public override long Position { get => _inner.Position; set => _inner.Position = value; }
        public override void Flush() => _inner.Flush();
        public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);
        public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override Task FlushAsync(CancellationToken cancellationToken) => _inner.FlushAsync(cancellationToken);
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
            _inner.ReadAsync(buffer, cancellationToken);

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _inner.Dispose();
                response.Dispose();
            }

            base.Dispose(disposing);
        }

        public override async ValueTask DisposeAsync()
        {
            await _inner.DisposeAsync();
            response.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}