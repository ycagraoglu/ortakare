namespace Ortakare.Api.Infrastructure.Observability;

public sealed class ObservabilityOptions
{
    public const string SectionName = "Observability";

    public bool Enabled { get; init; } = true;
    public string ServiceName { get; init; } = "ortakare-api";
    public string? OtlpEndpoint { get; init; }
    public double TraceSampleRatio { get; init; } = 0.1;
    public bool ExportSensitiveData { get; init; }
}
