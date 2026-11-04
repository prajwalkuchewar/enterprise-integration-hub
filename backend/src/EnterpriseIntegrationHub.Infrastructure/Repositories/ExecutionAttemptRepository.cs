using EnterpriseIntegrationHub.Application.Features.Executions.Worker;
using EnterpriseIntegrationHub.Application.Interfaces;
using EnterpriseIntegrationHub.Domain.Entities;
using EnterpriseIntegrationHub.Domain.Enums;
using EnterpriseIntegrationHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseIntegrationHub.Infrastructure.Repositories;

public sealed class ExecutionAttemptRepository(EnterpriseIntegrationHubDbContext context) : IExecutionAttemptRepository
{
    public async Task<BeginExecutionAttemptResult> BeginAsync(
        ExecutionLease lease, int maxAttempts, CancellationToken cancellationToken)
    {
        Validate(lease, maxAttempts);
        return await context.Database.CreateExecutionStrategy().ExecuteAsync(async ct =>
        {
            context.ChangeTracker.Clear();
            await using var transaction = await context.Database.BeginTransactionAsync(ct);
            var inbox = await LockLeaseAsync(lease, ct);
            if (inbox is null)
                return new BeginExecutionAttemptResult(BeginExecutionAttemptStatus.LeaseLost, null);

            var now = await DatabaseNowAsync(ct);
            var execution = await LoadExecutionAsync(lease.ExecutionId, ct);
            if (execution.Status is IntegrationExecutionStatus.Succeeded or IntegrationExecutionStatus.Failed or IntegrationExecutionStatus.Cancelled)
            {
                var finalized = await FinalizeInboxAsync(lease, ct);
                if (!finalized)
                    return new BeginExecutionAttemptResult(BeginExecutionAttemptStatus.LeaseLost, null);
                await transaction.CommitAsync(ct);
                return new BeginExecutionAttemptResult(BeginExecutionAttemptStatus.Completed, null);
            }

            var receipt = await context.EventReceipts.AsNoTracking()
                .SingleOrDefaultAsync(item => item.IntegrationExecutionId == execution.Id, ct);
            var runningStep = execution.Steps.SingleOrDefault(step => step.Status == ExecutionStepStatus.Running);
            if (runningStep is not null)
            {
                var runningAttempt = runningStep.Attempts.Single(attempt => attempt.Status == ExecutionStepAttemptStatus.Running);
                if (inbox.ActiveAttemptId != runningAttempt.Id || inbox.ActiveAttemptLeaseToken is null)
                    throw new InvalidDataException("Execution attempt and inbox state are inconsistent.");

                if (inbox.ActiveAttemptLeaseToken == lease.Token && receipt?.PayloadJson is not null && receipt.SchemaVersion is not null)
                {
                    await transaction.CommitAsync(ct);
                    return Ready(execution, runningStep, runningAttempt, receipt.PayloadJson);
                }

                var retryAt = runningStep.Attempts.Count >= maxAttempts ? (DateTimeOffset?)null : now;
                execution.MarkStepFailed(runningStep.ExecutionOrder, "INTERRUPTED",
                    "The previous destination attempt was interrupted; its outcome is unknown.", retryAt, now);
                if (!retryAt.HasValue)
                {
                    await context.SaveChangesAsync(ct);
                    if (!await FinalizeInboxAsync(lease, ct))
                        return new BeginExecutionAttemptResult(BeginExecutionAttemptStatus.LeaseLost, null);
                    await transaction.CommitAsync(ct);
                    return new BeginExecutionAttemptResult(BeginExecutionAttemptStatus.Completed, null);
                }
            }
            else if (inbox.ActiveAttemptId is not null)
            {
                throw new InvalidDataException("Inbox refers to an attempt that is not running.");
            }

            if (execution.Status is IntegrationExecutionStatus.Queued or IntegrationExecutionStatus.RetryScheduled)
                execution.Start(now);

            var next = execution.Steps.OrderBy(step => step.ExecutionOrder)
                .FirstOrDefault(step => step.Status is ExecutionStepStatus.Pending or ExecutionStepStatus.RetryScheduled)
                ?? throw new InvalidDataException("Execution has no available step.");
            var attempt = execution.StartStepAttempt(next.ExecutionOrder, now);
            if (receipt?.PayloadJson is null || receipt.SchemaVersion is null)
            {
                execution.MarkStepFailed(next.ExecutionOrder, "PAYLOAD_UNAVAILABLE",
                    "The accepted event payload is unavailable.", null, now);
                await context.SaveChangesAsync(ct);
                if (!await FinalizeInboxAsync(lease, ct))
                    return new BeginExecutionAttemptResult(BeginExecutionAttemptStatus.LeaseLost, null);
                await transaction.CommitAsync(ct);
                return new BeginExecutionAttemptResult(BeginExecutionAttemptStatus.Completed, null);
            }

            await context.SaveChangesAsync(ct);
            var updated = await context.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE [ExecutionDispatchInbox]
                SET [ActiveAttemptId] = {attempt.Id}, [ActiveAttemptLeaseToken] = {lease.Token}
                WHERE [DispatchId] = {lease.DispatchId} AND [IntegrationExecutionId] = {lease.ExecutionId}
                  AND [LeaseToken] = {lease.Token} AND [CompletedAt] IS NULL
                  AND [LeaseExpiresAt] > SYSDATETIMEOFFSET()
                """, ct);
            if (updated != 1)
                return new BeginExecutionAttemptResult(BeginExecutionAttemptStatus.LeaseLost, null);
            await transaction.CommitAsync(ct);
            return Ready(execution, next, attempt, receipt.PayloadJson);
        }, cancellationToken);
    }

    public async Task<FinishExecutionAttemptStatus> FinishAsync(
        ExecutionLease lease, Guid attemptId, ExecutionAttemptOutcome outcome, int maxAttempts, CancellationToken cancellationToken)
    {
        Validate(lease, maxAttempts);
        ArgumentNullException.ThrowIfNull(outcome);
        if (attemptId == Guid.Empty)
            throw new ArgumentException("AttemptId must be provided.", nameof(attemptId));
        if (outcome.HttpStatusCode is < 100 or > 599 || outcome.DurationMilliseconds is < 0)
            throw new ArgumentException("Attempt diagnostics are invalid.", nameof(outcome));
        if (outcome.Succeeded ? outcome.Failure is not null || outcome.RetryDelay is not null :
            outcome.Failure is null || !Enum.IsDefined(outcome.Failure.Value) ||
            (outcome.RetryDelay is not null && (outcome.Failure != ExecutionAttemptFailure.TransientDestination ||
                outcome.RetryDelay < TimeSpan.Zero || outcome.RetryDelay > TimeSpan.FromDays(30))))
            throw new ArgumentException("The attempt outcome is invalid.", nameof(outcome));

        return await context.Database.CreateExecutionStrategy().ExecuteAsync(async ct =>
        {
            context.ChangeTracker.Clear();
            await using var transaction = await context.Database.BeginTransactionAsync(ct);
            var inbox = await LockLeaseAsync(lease, ct);
            if (inbox is null)
                return FinishExecutionAttemptStatus.LeaseLost;
            if (inbox.ActiveAttemptId != attemptId || inbox.ActiveAttemptLeaseToken != lease.Token)
                throw new InvalidOperationException("The attempt is not owned by this lease.");

            var execution = await LoadExecutionAsync(lease.ExecutionId, ct);
            var now = await DatabaseNowAsync(ct);
            var step = execution.Steps.SingleOrDefault(item =>
                item.Status == ExecutionStepStatus.Running && item.Attempts.Any(attempt =>
                    attempt.Id == attemptId && attempt.Status == ExecutionStepAttemptStatus.Running))
                ?? throw new InvalidDataException("The active execution attempt was not found.");

            DateTimeOffset? retryAt = null;
            if (outcome.Succeeded)
                execution.MarkStepSucceeded(step.ExecutionOrder, now, outcome.HttpStatusCode, outcome.DurationMilliseconds);
            else
            {
                retryAt = step.Attempts.Count >= maxAttempts || outcome.RetryDelay is null
                    ? null
                    : now.Add(outcome.RetryDelay.Value);
                var (code, message) = FailureDiagnostic(outcome.Failure!.Value);
                execution.MarkStepFailed(step.ExecutionOrder, code, message, retryAt, now, outcome.HttpStatusCode, outcome.DurationMilliseconds);
            }

            await context.SaveChangesAsync(ct);
            var terminal = execution.Status is IntegrationExecutionStatus.Succeeded or IntegrationExecutionStatus.Failed or IntegrationExecutionStatus.Cancelled;
            int updated;
            if (terminal)
                updated = await FinalizeInboxAsync(lease, ct) ? 1 : 0;
            else if (retryAt.HasValue)
                updated = await context.Database.ExecuteSqlInterpolatedAsync($"""
                    UPDATE [ExecutionDispatchInbox]
                    SET [ActiveAttemptId] = NULL, [ActiveAttemptLeaseToken] = NULL,
                        [LeaseToken] = NULL, [LeaseExpiresAt] = NULL, [AvailableAt] = {retryAt}
                    WHERE [DispatchId] = {lease.DispatchId} AND [IntegrationExecutionId] = {lease.ExecutionId}
                      AND [LeaseToken] = {lease.Token} AND [CompletedAt] IS NULL
                      AND [LeaseExpiresAt] > SYSDATETIMEOFFSET()
                    """, ct);
            else
                updated = await context.Database.ExecuteSqlInterpolatedAsync($"""
                    UPDATE [ExecutionDispatchInbox]
                    SET [ActiveAttemptId] = NULL, [ActiveAttemptLeaseToken] = NULL
                    WHERE [DispatchId] = {lease.DispatchId} AND [IntegrationExecutionId] = {lease.ExecutionId}
                      AND [LeaseToken] = {lease.Token} AND [CompletedAt] IS NULL
                      AND [LeaseExpiresAt] > SYSDATETIMEOFFSET()
                    """, ct);
            if (updated != 1)
                return FinishExecutionAttemptStatus.LeaseLost;
            await transaction.CommitAsync(ct);
            return terminal ? FinishExecutionAttemptStatus.Completed
                : retryAt.HasValue ? FinishExecutionAttemptStatus.RetryScheduled
                : FinishExecutionAttemptStatus.ReadyForNextStep;
        }, cancellationToken);
    }

    private async Task<ExecutionDispatchInbox?> LockLeaseAsync(ExecutionLease lease, CancellationToken cancellationToken)
    {
        var matches = await context.ExecutionDispatchInbox.FromSqlInterpolated($"""
            SELECT * FROM [ExecutionDispatchInbox] WITH (UPDLOCK, HOLDLOCK)
            WHERE [DispatchId] = {lease.DispatchId} AND [IntegrationExecutionId] = {lease.ExecutionId}
              AND [LeaseToken] = {lease.Token} AND [CompletedAt] IS NULL
              AND [LeaseExpiresAt] > SYSDATETIMEOFFSET()
            """).AsNoTracking().ToListAsync(cancellationToken);
        return matches.SingleOrDefault();
    }

    private Task<IntegrationExecution> LoadExecutionAsync(Guid executionId, CancellationToken cancellationToken) =>
        context.IntegrationExecutions.Include(execution => execution.Steps).ThenInclude(step => step.Attempts)
            .SingleAsync(execution => execution.Id == executionId, cancellationToken);

    private Task<DateTimeOffset> DatabaseNowAsync(CancellationToken cancellationToken) =>
        context.Database.SqlQueryRaw<DateTimeOffset>("SELECT SYSDATETIMEOFFSET() AS [Value]").SingleAsync(cancellationToken);

    private async Task<bool> FinalizeInboxAsync(ExecutionLease lease, CancellationToken cancellationToken) =>
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE [ExecutionDispatchInbox]
            SET [CompletedAt] = SYSDATETIMEOFFSET(), [LeaseToken] = NULL, [LeaseExpiresAt] = NULL,
                [ActiveAttemptId] = NULL, [ActiveAttemptLeaseToken] = NULL
            WHERE [DispatchId] = {lease.DispatchId} AND [IntegrationExecutionId] = {lease.ExecutionId}
              AND [LeaseToken] = {lease.Token} AND [CompletedAt] IS NULL
              AND [LeaseExpiresAt] > SYSDATETIMEOFFSET()
            """, cancellationToken) == 1;

    private static BeginExecutionAttemptResult Ready(
        IntegrationExecution execution, ExecutionStep step, ExecutionStepAttempt attempt, string payload) =>
        new(BeginExecutionAttemptStatus.Ready, new ExecutionAttemptWork(
            execution.Id, step.Id, attempt.Id, attempt.AttemptNumber, step.ExecutionOrder,
            step.DestinationConnectorId, step.DestinationBaseUrl, step.Protocol, step.AuthenticationType,
            step.TimeoutSeconds, execution.CorrelationId, payload));

    private static (string Code, string Message) FailureDiagnostic(ExecutionAttemptFailure failure) => failure switch
    {
        ExecutionAttemptFailure.TransientDestination => ("DESTINATION_TRANSIENT", "The destination was temporarily unavailable."),
        ExecutionAttemptFailure.PermanentDestination => ("DESTINATION_PERMANENT", "The destination rejected the request."),
        ExecutionAttemptFailure.InvalidConfiguration => ("INVALID_CONFIGURATION", "The destination configuration is invalid."),
        _ => throw new ArgumentOutOfRangeException(nameof(failure))
    };

    private static void Validate(ExecutionLease lease, int maxAttempts)
    {
        ArgumentNullException.ThrowIfNull(lease);
        if (lease.DispatchId == Guid.Empty || lease.ExecutionId == Guid.Empty || lease.Token == Guid.Empty)
            throw new ArgumentException("The lease identity is invalid.", nameof(lease));
        if (maxAttempts < 1)
            throw new ArgumentOutOfRangeException(nameof(maxAttempts), "At least one attempt is required.");
    }
}
