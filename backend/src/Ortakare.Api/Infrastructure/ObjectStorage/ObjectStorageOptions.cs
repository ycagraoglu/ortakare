namespace Ortakare.Api.Infrastructure.ObjectStorage;

public sealed class ObjectStorageOptions
{
    public const string SectionName = "ObjectStorage";

    public string ServiceUrl { get; init; } = string.Empty;
    public string AccessKey { get; init; } = string.Empty;
    public string SecretKey { get; init; } = string.Empty;
    public string BucketName { get; init; } = string.Empty;
    public int SignedUrlMinutes { get; init; } = 10;
}