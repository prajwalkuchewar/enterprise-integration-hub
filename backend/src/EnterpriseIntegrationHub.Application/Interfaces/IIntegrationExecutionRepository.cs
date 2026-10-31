using EnterpriseIntegrationHub.Domain.Entities;

namespace EnterpriseIntegrationHub.Application.Interfaces;

public interface IIntegrationExecutionRepository
{
    Task<IntegrationExecution?> GetBySourceEventAsync(Guid sourceConnectorId, string sourceEventId, CancellationToken cancellationToken);
    Task<IntegrationExecution> CreateIfAbsentAsync(IntegrationExecution execution, ExecutionDispatchOutbox dispatch, CancellationToken cancellationToken);
}
