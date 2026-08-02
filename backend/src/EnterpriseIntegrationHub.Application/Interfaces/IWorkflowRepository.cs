using EnterpriseIntegrationHub.Domain.Entities;

namespace EnterpriseIntegrationHub.Application.Interfaces;

public interface IWorkflowRepository
{
    Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken);
    Task AddAsync(Workflow workflow, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<Workflow>> GetAllAsync(CancellationToken cancellationToken);
}
