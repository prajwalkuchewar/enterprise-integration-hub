using EnterpriseIntegrationHub.Application.Interfaces;
using EnterpriseIntegrationHub.Domain.Entities;
using EnterpriseIntegrationHub.Domain.Enums;
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
        try { await context.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new InvalidOperationException("The workflow changed after it was loaded. Reload it and retry the update.", exception);
        }
    }

    public async Task<IReadOnlyCollection<Workflow>> GetAllAsync(CancellationToken cancellationToken) =>
        await context.Workflows
            .AsNoTracking()
            .Include(x => x.Steps)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<Workflow>> GetActiveForSourceConnectorAsync(Guid sourceConnectorId, CancellationToken cancellationToken) =>
        await context.Workflows
            .AsNoTracking()
            .Where(workflow => workflow.Status == WorkflowStatus.Active && workflow.SourceConnectorId == sourceConnectorId)
            .Include(workflow => workflow.Steps)
            .ToListAsync(cancellationToken);

    public Task<bool> HasActiveWorkflowForConnectorAsync(Guid connectorId, CancellationToken cancellationToken) =>
        context.Workflows.AnyAsync(
            workflow => workflow.Status == WorkflowStatus.Active &&
                (workflow.SourceConnectorId == connectorId || workflow.Steps.Any(step => step.DestinationConnectorId == connectorId)),
            cancellationToken);

    public Task<bool> HasActiveWorkflowForExternalSystemAsync(Guid externalSystemId, CancellationToken cancellationToken) =>
        context.Workflows.AnyAsync(
            workflow => workflow.Status == WorkflowStatus.Active &&
                (context.Connectors.Any(connector => connector.ExternalSystemId == externalSystemId && connector.Id == workflow.SourceConnectorId) ||
                 workflow.Steps.Any(step => context.Connectors.Any(connector =>
                     connector.ExternalSystemId == externalSystemId && connector.Id == step.DestinationConnectorId))),
            cancellationToken);
}
