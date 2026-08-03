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
        context.Workflows.AsNoTracking().Include(x => x.Steps).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task UpdateAsync(Workflow workflow, CancellationToken cancellationToken)
    {
        context.Workflows.Update(workflow);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<Workflow>> GetAllAsync(CancellationToken cancellationToken) =>
        await context.Workflows
            .AsNoTracking()
            .Include(x => x.Steps)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
}
