using EnterpriseIntegrationHub.Application.Features.Executions.DispatchOutbox;
using EnterpriseIntegrationHub.Application.Interfaces;
using EnterpriseIntegrationHub.Domain.Entities;
using EnterpriseIntegrationHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseIntegrationHub.Infrastructure.Repositories;

public sealed class ExecutionDispatchOutboxRepository(EnterpriseIntegrationHubDbContext context) : IExecutionDispatchOutboxRepository
{
    public async Task<IReadOnlyList<ExecutionDispatchMessage>> ClaimBatchAsync(
        string leaseOwner,
        DateTimeOffset now,
        TimeSpan leaseDuration,
        int batchSize,
        CancellationToken cancellationToken)
    {
        if (batchSize < 1)
            throw new ArgumentOutOfRangeException(nameof(batchSize), "The batch size must be at least one.");

        // READPAST lets concurrent workers skip rows another worker is leasing instead of blocking on them.
        var claimed = await context.Database.CreateExecutionStrategy().ExecuteAsync(async ct =>
        {
            await using var transaction = await context.Database.BeginTransactionAsync(ct);
            var candidates = await context.ExecutionDispatchOutbox
                .FromSql($"""
                    SELECT TOP ({batchSize}) *
                    FROM ExecutionDispatchOutbox WITH (UPDLOCK, READPAST, ROWLOCK)
                    WHERE DispatchedAt IS NULL
                      AND DeadLetteredAt IS NULL
                      AND NextAttemptAt <= {now}
                      AND (LeaseExpiresAt IS NULL OR LeaseExpiresAt <= {now})
                    ORDER BY NextAttemptAt
                    """)
                .ToListAsync(ct);

            foreach (var candidate in candidates)
                candidate.Lease(leaseOwner, now, leaseDuration);

            await context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return candidates;
        }, cancellationToken);

        if (claimed.Count == 0)
            return [];

        var executionIds = claimed.Select(candidate => candidate.IntegrationExecutionId).ToList();
        var executions = await context.IntegrationExecutions
            .AsNoTracking()
            .Where(execution => executionIds.Contains(execution.Id))
            .Select(execution => new
            {
                execution.Id,
                execution.WorkflowId,
                execution.WorkflowRevisionNumber,
                execution.SourceConnectorId,
                execution.SourceEventId,
                execution.TriggerEvent,
                execution.OccurredAt,
                execution.CorrelationId,
            })
            .ToDictionaryAsync(execution => execution.Id, cancellationToken);

        return claimed
            .Select(outbox =>
            {
                var execution = executions[outbox.IntegrationExecutionId];
                return new ExecutionDispatchMessage(
                    outbox.Id,
                    outbox.AttemptCount,
                    execution.Id,
                    execution.WorkflowId,
                    execution.WorkflowRevisionNumber,
                    execution.SourceConnectorId,
                    execution.SourceEventId,
                    execution.TriggerEvent,
                    execution.OccurredAt,
                    execution.CorrelationId);
            })
            .ToList();
    }

    public async Task<bool> MarkDispatchedAsync(Guid dispatchId, string leaseOwner, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var current = await LoadLeasedAsync(dispatchId, leaseOwner, cancellationToken);
        if (current is null)
            return false;

        current.MarkDispatched(leaseOwner, now);

        // The lease owner is part of the predicate, so a worker whose lease was reclaimed cannot overwrite the new lease.
        var updated = await context.ExecutionDispatchOutbox
            .Where(outbox => outbox.Id == dispatchId && outbox.LeaseOwner == leaseOwner)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(outbox => outbox.DispatchedAt, current.DispatchedAt)
                .SetProperty(outbox => outbox.LeaseOwner, current.LeaseOwner)
                .SetProperty(outbox => outbox.LeaseExpiresAt, current.LeaseExpiresAt)
                .SetProperty(outbox => outbox.LastError, current.LastError), cancellationToken);

        return updated == 1;
    }

    public async Task<bool> RecordFailureAsync(
        Guid dispatchId,
        string leaseOwner,
        DateTimeOffset now,
        string error,
        TimeSpan retryDelay,
        int maxAttempts,
        CancellationToken cancellationToken)
    {
        var current = await LoadLeasedAsync(dispatchId, leaseOwner, cancellationToken);
        if (current is null)
            return false;

        current.RecordFailure(leaseOwner, now, error, retryDelay, maxAttempts);

        var updated = await context.ExecutionDispatchOutbox
            .Where(outbox => outbox.Id == dispatchId && outbox.LeaseOwner == leaseOwner)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(outbox => outbox.LeaseOwner, current.LeaseOwner)
                .SetProperty(outbox => outbox.LeaseExpiresAt, current.LeaseExpiresAt)
                .SetProperty(outbox => outbox.NextAttemptAt, current.NextAttemptAt)
                .SetProperty(outbox => outbox.LastError, current.LastError)
                .SetProperty(outbox => outbox.DeadLetteredAt, current.DeadLetteredAt), cancellationToken);

        return updated == 1;
    }

    private Task<ExecutionDispatchOutbox?> LoadLeasedAsync(Guid dispatchId, string leaseOwner, CancellationToken cancellationToken) =>
        context.ExecutionDispatchOutbox
            .AsNoTracking()
            .FirstOrDefaultAsync(outbox => outbox.Id == dispatchId
                                           && outbox.LeaseOwner == leaseOwner
                                           && outbox.DispatchedAt == null, cancellationToken);
}
