using EnterpriseIntegrationHub.Application.Interfaces;
using System.Diagnostics;

namespace EnterpriseIntegrationHub.Application.Features.Executions.Worker;

public enum ExecutionWorkerRunStatus
{
    Idle,
    Finished,
    RetryScheduled,
    LeaseLost
}

public sealed class ExecutionWorkerHandler(
    IExecutionLeaseRepository leases,
    IExecutionAttemptRepository attempts,
    IExecutionDestinationInvoker destination)
{
    private const int LeaseSafetyMarginSeconds = 30;

    public async Task<ExecutionWorkerRunStatus> RunOnceAsync(
        ExecutionWorkerOptions options, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();
        var lease = await leases.ClaimAsync(options.LeaseSeconds, cancellationToken);
        if (lease is null)
            return ExecutionWorkerRunStatus.Idle;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!await leases.RenewAsync(lease.DispatchId, lease.Token, options.LeaseSeconds, cancellationToken))
                return ExecutionWorkerRunStatus.LeaseLost;
            var prepared = await attempts.BeginAsync(lease, options.MaxAttempts, cancellationToken);
            if (prepared.Status == BeginExecutionAttemptStatus.LeaseLost)
                return ExecutionWorkerRunStatus.LeaseLost;
            if (prepared.Status == BeginExecutionAttemptStatus.Completed)
                return ExecutionWorkerRunStatus.Finished;
            if (prepared.Status != BeginExecutionAttemptStatus.Ready || prepared.Work is null)
                throw new InvalidDataException("Execution attempt preparation returned an invalid result.");

            var work = prepared.Work;
            DestinationInvocationResult result;
            if (work.TimeoutSeconds <= 0 || work.TimeoutSeconds > 86400 - LeaseSafetyMarginSeconds)
                result = DestinationInvocationResult.InvalidConfiguration;
            else
            {
                var callLeaseSeconds = Math.Max(options.LeaseSeconds, work.TimeoutSeconds + LeaseSafetyMarginSeconds);
                if (!await leases.RenewAsync(lease.DispatchId, lease.Token, callLeaseSeconds, cancellationToken))
                    return ExecutionWorkerRunStatus.LeaseLost;
                using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                deadline.CancelAfter(TimeSpan.FromSeconds(work.TimeoutSeconds));
                var timer = Stopwatch.StartNew();
                try
                {
                    result = await destination.InvokeAsync(work, deadline.Token);
                }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested && deadline.IsCancellationRequested)
                {
                    result = new DestinationInvocationResult(DestinationInvocationStatus.TransientFailure,
                        DurationMilliseconds: timer.ElapsedMilliseconds);
                }
            }

            cancellationToken.ThrowIfCancellationRequested();
            var outcome = result.Status switch
            {
                DestinationInvocationStatus.Succeeded => new ExecutionAttemptOutcome(true, null, null, result.HttpStatusCode, result.DurationMilliseconds),
                DestinationInvocationStatus.TransientFailure => new ExecutionAttemptOutcome(false,
                    ExecutionAttemptFailure.TransientDestination,
                    work.AttemptNumber < options.MaxAttempts ? CalculateRetryDelay(work.AttemptNumber, options) : null,
                    result.HttpStatusCode, result.DurationMilliseconds),
                DestinationInvocationStatus.PermanentFailure => new ExecutionAttemptOutcome(false, ExecutionAttemptFailure.PermanentDestination, null, result.HttpStatusCode, result.DurationMilliseconds),
                DestinationInvocationStatus.InvalidConfiguration => new ExecutionAttemptOutcome(false, ExecutionAttemptFailure.InvalidConfiguration, null, result.HttpStatusCode, result.DurationMilliseconds),
                _ => throw new InvalidDataException("Destination invocation returned an invalid result.")
            };

            var finished = await attempts.FinishAsync(lease, work.AttemptId, outcome, options.MaxAttempts, cancellationToken);
            switch (finished)
            {
                case FinishExecutionAttemptStatus.LeaseLost:
                    return ExecutionWorkerRunStatus.LeaseLost;
                case FinishExecutionAttemptStatus.Completed:
                    return ExecutionWorkerRunStatus.Finished;
                case FinishExecutionAttemptStatus.RetryScheduled when outcome.RetryDelay.HasValue:
                    return ExecutionWorkerRunStatus.RetryScheduled;
                case FinishExecutionAttemptStatus.ReadyForNextStep when outcome.Succeeded:
                    break;
                default:
                    throw new InvalidDataException("Execution attempt completion returned an invalid result.");
            }
        }
    }

    public static TimeSpan CalculateRetryDelay(int attemptNumber, ExecutionWorkerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();
        if (attemptNumber < 1)
            throw new ArgumentOutOfRangeException(nameof(attemptNumber));
        var exponent = Math.Min(attemptNumber - 1, 30);
        var capSeconds = Math.Min(options.MaxRetryDelaySeconds, options.BaseRetryDelaySeconds * Math.Pow(2, exponent));
        return TimeSpan.FromSeconds(Random.Shared.NextDouble() * capSeconds);
    }
}
