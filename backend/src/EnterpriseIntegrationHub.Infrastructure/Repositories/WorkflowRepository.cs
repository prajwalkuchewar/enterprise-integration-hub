using EnterpriseIntegrationHub.Application.Interfaces;
using EnterpriseIntegrationHub.Domain.Entities;
using EnterpriseIntegrationHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseIntegrationHub.Infrastructure.Repositories;

public sealed class WorkflowRepository(EnterpriseIntegrationHubDbContext context) : IWorkflowRepository
{
    public Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken) =>
        context.Workflows.AnyAsync(x => x.Name == name, cancellationToken);

    public async Task AddAsync(Workflow workflow, CancellationToken cancellationToken)
    {
        context.Workflows.Add(workflow);
        await context.SaveChangesAsync(cancellationToken);
    }
}
