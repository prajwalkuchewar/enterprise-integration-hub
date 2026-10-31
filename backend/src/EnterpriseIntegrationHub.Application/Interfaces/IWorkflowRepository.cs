using EnterpriseIntegrationHub.Domain.Entities;

namespace EnterpriseIntegrationHub.Application.Interfaces;

public interface IWorkflowRepository
{
    Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken, Guid? excludedWorkflowId = null);
    Task AddAsync(Workflow workflow, CancellationToken cancellationToken);
    Task<Workflow?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task UpdateAsync(Workflow workflow, CancellationToken cancellationToken);
    Task<WorkflowRevision?> GetRevisionByIdAsync(Guid revisionId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<Workflow>> GetAllAsync(CancellationToken cancellationToken);
    Task<IReadOnlyCollection<WorkflowRevision>> GetActiveRevisionsForSourceConnectorAsync(Guid sourceConnectorId, CancellationToken cancellationToken);
    Task<bool> HasActiveWorkflowForConnectorAsync(Guid connectorId, CancellationToken cancellationToken);
    Task<bool> HasActiveWorkflowForExternalSystemAsync(Guid externalSystemId, CancellationToken cancellationToken);
}
