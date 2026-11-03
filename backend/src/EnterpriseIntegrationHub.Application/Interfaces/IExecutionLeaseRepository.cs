using EnterpriseIntegrationHub.Application.Features.Executions.Worker;

namespace EnterpriseIntegrationHub.Application.Interfaces;

public interface IExecutionLeaseRepository
{
    Task<ExecutionLease?> ClaimAsync(int leaseSeconds, CancellationToken cancellationToken);
    Task<bool> RenewAsync(Guid dispatchId, Guid token, int leaseSeconds, CancellationToken cancellationToken);
    Task<bool> ReleaseAsync(Guid dispatchId, Guid token, DateTimeOffset? availableAt, CancellationToken cancellationToken);
    Task<bool> CompleteAsync(Guid dispatchId, Guid token, CancellationToken cancellationToken);
}
