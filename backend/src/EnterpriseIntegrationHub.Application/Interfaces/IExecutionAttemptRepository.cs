using EnterpriseIntegrationHub.Application.Features.Executions.Worker;

namespace EnterpriseIntegrationHub.Application.Interfaces;

public interface IExecutionAttemptRepository
{
    Task<BeginExecutionAttemptResult> BeginAsync(ExecutionLease lease, int maxAttempts, CancellationToken cancellationToken);
    Task<FinishExecutionAttemptStatus> FinishAsync(ExecutionLease lease, Guid attemptId, ExecutionAttemptOutcome outcome, int maxAttempts, CancellationToken cancellationToken);
}
