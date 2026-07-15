namespace Ortakare.Api.Infrastructure.BackgroundJobs;

public sealed class BackgroundJobShutdownOptions
{
    public const string SectionName = "Hangfire:Shutdown";

    public int HostShutdownSeconds { get; init; } = 90;
    public int StopSeconds { get; init; } = 60;
    public int ShutdownSeconds { get; init; } = 75;
    public int CancellationCheckSeconds { get; init; } = 2;
}
