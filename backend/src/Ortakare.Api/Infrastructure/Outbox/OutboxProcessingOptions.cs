namespace Ortakare.Api.Infrastructure.Outbox;

public sealed class OutboxProcessingOptions
{
    public const string SectionName = "Outbox";

    public bool Enabled { get; init; } = true;
    public int PollingIntervalSeconds { get; init; } = 10;
    public int BatchSize { get; init; } = 50;
    public int MaxRetryCount { get; init; } = 8;
    public int InitialRetryDelaySeconds { get; init; } = 30;
    public int MaxRetryDelaySeconds { get; init; } = 3600;
    public int LockTimeoutSeconds { get; init; } = 300;
}