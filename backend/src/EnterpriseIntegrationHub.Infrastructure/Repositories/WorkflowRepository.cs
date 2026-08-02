using EnterpriseIntegrationHub.Application.Interfaces;
using EnterpriseIntegrationHub.Domain.Entities;
using EnterpriseIntegrationHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseIntegrationHub.Infrastructure.Repositories;

public sealed class WorkflowRepository(EnterpriseIntegrationHubDbContext context) : IWorkflowRepository
{
    public Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken, Guid? excludedWorkflowId = null) =>
        context.Workflows.AnyAsync(x => x.Name == name && (excludedWorkflowId == null || x.Id != excludedWorkflowId), cancellationToken);

    public async Task AddAsync(Workflow workflow, CancellationToken cancellationToken)
    {
        context.Workflows.Add(workflow);
        await context.SaveChangesAsync(cancellationToken);
    }

    public Task<Workflow?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        context.Workflows.Include(x => x.Steps).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task UpdateAsync(Workflow workflow, CancellationToken cancellationToken) => context.SaveChangesAsync(cancellationToken);
}
