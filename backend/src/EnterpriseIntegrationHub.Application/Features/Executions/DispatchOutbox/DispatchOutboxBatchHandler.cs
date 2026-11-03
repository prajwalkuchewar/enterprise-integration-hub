using EnterpriseIntegrationHub.Application.Interfaces;

namespace EnterpriseIntegrationHub.Application.Features.Executions.DispatchOutbox;

public sealed class DispatchOutboxBatchHandler(
    IExecutionDispatchOutboxRepository outboxRepository,
    IExecutionDispatchPublisher publisher)
{
    public async Task<DispatchOutboxBatchResult> DispatchBatchAsync(
        DispatchOutboxOptions options,
        string leaseOwner,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(options);

        // The claim commits before any publish, so no database transaction is held while Kafka is called.
        var claimed = await outboxRepository.ClaimBatchAsync(
            leaseOwner,
            now,
            TimeSpan.FromSeconds(options.LeaseSeconds),
            options.BatchSize,
            cancellationToken);

        var dispatched = 0;
        var failed = 0;
        var leaseLost = 0;

        foreach (var message in claimed)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await publisher.PublishAsync(message, cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                var recorded = await outboxRepository.RecordFailureAsync(
                    message.DispatchId,
                    leaseOwner,
                    DateTimeOffset.UtcNow,
                    exception.Message,
                    ComputeRetryDelay(message.Attempt, options),
                    options.MaxAttempts,
                    cancellationToken);

                if (recorded) failed++; else leaseLost++;
                continue;
            }

            var marked = await outboxRepository.MarkDispatchedAsync(
                message.DispatchId,
                leaseOwner,
                DateTimeOffset.UtcNow,
                cancellationToken);

            if (marked) dispatched++; else leaseLost++;
        }

        return new DispatchOutboxBatchResult(claimed.Count, dispatched, failed, leaseLost);
    }

    public static TimeSpan ComputeRetryDelay(int attempt, DispatchOutboxOptions options)
    {
        var exponent = Math.Clamp(attempt - 1, 0, 20);
        var seconds = options.BaseRetryDelaySeconds * Math.Pow(2, exponent);
        return TimeSpan.FromSeconds(Math.Min(seconds, options.MaxRetryDelaySeconds));
    }
}

public sealed record DispatchOutboxBatchResult(int Claimed, int Dispatched, int Failed, int LeaseLost);
