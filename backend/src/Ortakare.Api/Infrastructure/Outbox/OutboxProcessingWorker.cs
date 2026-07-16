using Microsoft.Extensions.Options;

namespace Ortakare.Api.Infrastructure.Outbox;

public sealed class OutboxProcessingWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<OutboxProcessingOptions> options,
    ILogger<OutboxProcessingWorker> logger) : BackgroundService
{
    private readonly OutboxProcessingOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Outbox processing worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var processor = scope.ServiceProvider.GetRequiredService<OutboxProcessor>();
                await processor.ProcessBatchAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Outbox processing cycle failed.");
            }

            await Task.Delay(
                TimeSpan.FromSeconds(_options.PollingIntervalSeconds),
                stoppingToken);
        }

        logger.LogInformation("Outbox processing worker stopped.");
    }
}
