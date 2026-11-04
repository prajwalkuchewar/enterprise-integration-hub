using EnterpriseIntegrationHub.Application.Features.Executions.Worker;
using EnterpriseIntegrationHub.Application.Interfaces;
using EnterpriseIntegrationHub.Api.Delivery;
using Microsoft.Extensions.Options;

namespace EnterpriseIntegrationHub.Api.Hosting;

/// <summary>Processes durable execution dispatches when explicitly enabled.</summary>
public sealed class ExecutionWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<ExecutionWorkerOptions> options,
    ILogger<ExecutionWorker> logger) : BackgroundService
{
    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();
        var settings = options.Value;
        if (!settings.Enabled)
            return;
        settings.Validate();
        await using (var validationScope = scopeFactory.CreateAsyncScope())
        {
            validationScope.ServiceProvider.GetRequiredService<DestinationNetworkPolicy>().EnsureConfigured();
            _ = validationScope.ServiceProvider.GetRequiredService<IExecutionDestinationInvoker>();
        }

        logger.LogInformation("Execution worker started.");
        while (!stoppingToken.IsCancellationRequested)
        {
            var wait = true;
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var handler = scope.ServiceProvider.GetRequiredService<ExecutionWorkerHandler>();
                var result = await handler.RunOnceAsync(settings, stoppingToken);
                wait = result == ExecutionWorkerRunStatus.Idle;
                if (result == ExecutionWorkerRunStatus.LeaseLost)
                    logger.LogWarning("Execution worker lost a lease; its result was not committed.");
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError("Execution worker iteration failed ({ErrorType}); retrying after the poll interval.",
                    exception.GetType().Name);
            }

            if (wait)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(settings.PollIntervalSeconds), stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }
        logger.LogInformation("Execution worker stopped.");
    }
}
