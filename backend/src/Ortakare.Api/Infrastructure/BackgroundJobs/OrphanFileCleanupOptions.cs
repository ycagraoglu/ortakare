namespace Ortakare.Api.Infrastructure.BackgroundJobs;

public sealed class OrphanFileCleanupOptions
{
    public const string SectionName = "OrphanFileCleanup";

    public bool Enabled { get; init; } = true;
    public int IntervalHours { get; init; } = 24;
    public int GracePeriodHours { get; init; } = 24;
    public int MaxObjectsPerPrefix { get; init; } = 5000;
    public int MaxDeletesPerRun { get; init; } = 200;
}