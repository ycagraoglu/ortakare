using Microsoft.Extensions.Options;

namespace Ortakare.Api.Infrastructure.BackgroundJobs;

public sealed class GalleryExportCleanupWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<GalleryExportCleanupOptions> options,
    ILogger<GalleryExportCleanupWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Enabled)
        {
            logger.LogInformation("Gallery export cleanup worker is disabled.");
            return;
        }

        await RunCleanupAsync(stoppingToken);

        using var timer = new PeriodicTimer(
            TimeSpan.FromMinutes(options.Value.IntervalMinutes));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunCleanupAsync(stoppingToken);
        }
    }

    private async Task RunCleanupAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var cleanupJob = scope.ServiceProvider
                .GetRequiredService<CleanupExpiredGalleryExportsJob>();

            var result = await cleanupJob.ExecuteAsync(cancellationToken);

            logger.LogInformation(
                "Gallery export cleanup completed. Scanned: {ScannedCount}, Deleted: {DeletedCount}, Failed: {FailedCount}.",
                result.ScannedCount,
                result.DeletedCount,
                result.FailedCount);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Expected during application shutdown.
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Gallery export cleanup worker execution failed.");
        }
    }
}
