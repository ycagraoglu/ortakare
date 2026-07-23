using Microsoft.Extensions.Options;

namespace Ortakare.Api.Infrastructure.BackgroundJobs;

public sealed class OrphanFileCleanupWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<OrphanFileCleanupOptions> options,
    ILogger<OrphanFileCleanupWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Enabled)
        {
            logger.LogInformation("Orphan file cleanup worker is disabled.");
            return;
        }

        await RunCleanupAsync(stoppingToken);

        using var timer = new PeriodicTimer(
            TimeSpan.FromHours(options.Value.IntervalHours));

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
            var job = scope.ServiceProvider.GetRequiredService<CleanupOrphanFilesJob>();
            var result = await job.ExecuteAsync(cancellationToken);

            logger.LogInformation(
                "Orphan file cleanup completed. Scanned: {ScannedCount}, Orphans: {OrphanCount}, Deleted: {DeletedCount}, Failed: {FailedCount}.",
                result.ScannedCount,
                result.OrphanCount,
                result.DeletedCount,
                result.FailedCount);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Orphan file cleanup worker execution failed.");
        }
    }
}