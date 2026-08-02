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

    public async Task<IReadOnlyCollection<Workflow>> GetAllAsync(CancellationToken cancellationToken) =>
        await context.Workflows
            .AsNoTracking()
            .Include(x => x.Steps)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
}
