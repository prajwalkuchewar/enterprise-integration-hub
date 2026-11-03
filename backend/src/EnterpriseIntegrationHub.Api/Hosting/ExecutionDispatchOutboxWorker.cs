using EnterpriseIntegrationHub.Application.Features.Executions.DispatchOutbox;
using Microsoft.Extensions.Options;

namespace EnterpriseIntegrationHub.Api.Hosting;

/// <summary>Polls the execution dispatch outbox and publishes claimed rows to Kafka.</summary>
public sealed class ExecutionDispatchOutboxWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<DispatchOutboxOptions> options,
    ILogger<ExecutionDispatchOutboxWorker> logger) : BackgroundService
{
    // Unique per process so a restarted or concurrent instance never mistakes another worker's lease for its own.
    private readonly string _leaseOwner = $"{Environment.MachineName}-{Guid.NewGuid():N}";

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;
        logger.LogInformation(
            "Execution dispatch outbox worker started as {LeaseOwner} (batch {BatchSize}, lease {LeaseSeconds}s).",
            _leaseOwner,
            settings.BatchSize,
            settings.LeaseSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            var idle = true;
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var handler = scope.ServiceProvider.GetRequiredService<DispatchOutboxBatchHandler>();
                var result = await handler.DispatchBatchAsync(settings, _leaseOwner, DateTimeOffset.UtcNow, stoppingToken);

                idle = result.Claimed == 0;
                if (!idle)
                {
                    logger.LogInformation(
                        "Outbox batch: claimed {Claimed}, dispatched {Dispatched}, failed {Failed}, lease lost {LeaseLost}.",
                        result.Claimed,
                        result.Dispatched,
                        result.Failed,
                        result.LeaseLost);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Execution dispatch outbox batch failed; retrying after the poll interval.");
            }

            if (idle)
                await Task.Delay(TimeSpan.FromSeconds(settings.PollIntervalSeconds), stoppingToken);
        }

        logger.LogInformation("Execution dispatch outbox worker stopped.");
    }
}
