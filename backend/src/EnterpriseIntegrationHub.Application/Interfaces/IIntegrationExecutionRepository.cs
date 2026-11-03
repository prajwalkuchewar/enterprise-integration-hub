using EnterpriseIntegrationHub.Domain.Entities;

namespace EnterpriseIntegrationHub.Application.Interfaces;

public interface IIntegrationExecutionRepository
{
    Task<IntegrationExecution?> GetBySourceEventAsync(Guid sourceConnectorId, string sourceEventId, CancellationToken cancellationToken);
    /// <summary>
    /// Persists the event receipt, execution, and dispatch outbox row in one transaction, or returns the
    /// execution already recorded for the same source event.
    /// </summary>
    Task<IntegrationExecution> CreateIfAbsentAsync(
        EventReceipt receipt,
        IntegrationExecution execution,
        ExecutionDispatchOutbox dispatch,
        CancellationToken cancellationToken);
}
