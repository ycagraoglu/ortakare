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
}