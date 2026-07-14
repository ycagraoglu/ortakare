using Hangfire;
using Ortakare.Api.Features.GalleryExports;

namespace Ortakare.Api.Infrastructure.BackgroundJobs;

public sealed class HangfireGalleryExportJobScheduler(
    IBackgroundJobClient backgroundJobClient) : IGalleryExportJobScheduler
{
    public string Enqueue(Guid exportId) =>
        backgroundJobClient.Enqueue<BuildGalleryExportJob>(
            job => job.ExecuteAsync(exportId, CancellationToken.None));
}