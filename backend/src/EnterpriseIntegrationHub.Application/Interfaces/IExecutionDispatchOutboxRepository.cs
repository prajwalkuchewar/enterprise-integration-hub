namespace EnterpriseIntegrationHub.Application.Interfaces;

using EnterpriseIntegrationHub.Application.Features.Executions.DispatchOutbox;

public interface IExecutionDispatchOutboxRepository
{
    Task<IReadOnlyList<ExecutionDispatchMessage>> ClaimBatchAsync(
        string leaseOwner,
        DateTimeOffset now,
        TimeSpan leaseDuration,
        int batchSize,
        CancellationToken cancellationToken);

    Task<bool> MarkDispatchedAsync(Guid dispatchId, string leaseOwner, DateTimeOffset now, CancellationToken cancellationToken);

    Task<bool> RecordFailureAsync(
        Guid dispatchId,
        string leaseOwner,
        DateTimeOffset now,
        string error,
        TimeSpan retryDelay,
        int maxAttempts,
        CancellationToken cancellationToken);
}
