namespace Ortakare.Api.Infrastructure.HealthChecks;

public sealed class HealthCheckOptions
{
    public const string SectionName = "HealthChecks";

    public int DependencyTimeoutSeconds { get; init; } = 3;
    public int SlowCheckThresholdMilliseconds { get; init; } = 1000;
}
