namespace Ortakare.Api.Infrastructure.RateLimiting;

public sealed class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    public int AuthPermitLimit { get; set; } = 10;
    public int PublicPermitLimit { get; set; } = 120;
    public int UploadPermitLimit { get; set; } = 30;
    public int OwnerPermitLimit { get; set; } = 300;
    public int WindowSeconds { get; set; } = 60;
}
