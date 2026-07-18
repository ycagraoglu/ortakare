using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Ortakare.Api.Infrastructure.Observability;

public sealed class OrtakareTelemetry
{
    public const string ActivitySourceName = "Ortakare.Api";
    public const string MeterName = "Ortakare.Api";

    private readonly Meter _meter = new(MeterName);

    public ActivitySource ActivitySource { get; } = new(ActivitySourceName);
    public Counter<long> OutboxProcessed { get; }
    public Counter<long> OutboxFailed { get; }
    public Histogram<double> OutboxDurationMilliseconds { get; }
    public Counter<long> UploadAccepted { get; }
    public Counter<long> UploadRejected { get; }
    public Histogram<long> UploadSizeBytes { get; }
    public Counter<long> BackgroundJobSucceeded { get; }
    public Counter<long> BackgroundJobFailed { get; }
    public Histogram<double> BackgroundJobDurationMilliseconds { get; }

    public OrtakareTelemetry()
    {
        OutboxProcessed = _meter.CreateCounter<long>("ortakare.outbox.processed", unit: "{message}");
        OutboxFailed = _meter.CreateCounter<long>("ortakare.outbox.failed", unit: "{message}");
        OutboxDurationMilliseconds = _meter.CreateHistogram<double>("ortakare.outbox.duration", unit: "ms");
        UploadAccepted = _meter.CreateCounter<long>("ortakare.upload.accepted", unit: "{file}");
        UploadRejected = _meter.CreateCounter<long>("ortakare.upload.rejected", unit: "{file}");
        UploadSizeBytes = _meter.CreateHistogram<long>("ortakare.upload.size", unit: "By");
        BackgroundJobSucceeded = _meter.CreateCounter<long>("ortakare.background_job.succeeded", unit: "{job}");
        BackgroundJobFailed = _meter.CreateCounter<long>("ortakare.background_job.failed", unit: "{job}");
        BackgroundJobDurationMilliseconds = _meter.CreateHistogram<double>("ortakare.background_job.duration", unit: "ms");
    }
}
