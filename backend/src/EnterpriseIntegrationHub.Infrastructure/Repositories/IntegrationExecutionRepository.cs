using EnterpriseIntegrationHub.Application.Interfaces;
using EnterpriseIntegrationHub.Domain.Entities;
using EnterpriseIntegrationHub.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseIntegrationHub.Infrastructure.Repositories;

public sealed class IntegrationExecutionRepository(EnterpriseIntegrationHubDbContext context) : IIntegrationExecutionRepository
{
    public Task<IntegrationExecution?> GetBySourceEventAsync(Guid sourceConnectorId, string sourceEventId, CancellationToken cancellationToken) =>
        FindBySourceEventAsync(sourceConnectorId, sourceEventId, cancellationToken);

    public async Task<IntegrationExecution> CreateIfAbsentAsync(
        IntegrationExecution execution,
        ExecutionDispatchOutbox dispatch,
        CancellationToken cancellationToken)
    {
        var existing = await FindBySourceEventAsync(execution.SourceConnectorId, execution.SourceEventId, cancellationToken);
        if (existing is not null)
            return existing;

        context.IntegrationExecutions.Add(execution);
        context.ExecutionDispatchOutbox.Add(dispatch);
        try
        {
            await context.SaveChangesAsync(cancellationToken);
            return execution;
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            context.ChangeTracker.Clear();
            var racedExecution = await FindBySourceEventAsync(execution.SourceConnectorId, execution.SourceEventId, cancellationToken);
            if (racedExecution is not null)
                return racedExecution;
            throw;
        }
    }

    private Task<IntegrationExecution?> FindBySourceEventAsync(Guid sourceConnectorId, string sourceEventId, CancellationToken cancellationToken) =>
        context.IntegrationExecutions
            .AsNoTracking()
            .Include(execution => execution.Steps)
            .ThenInclude(step => step.Attempts)
            .FirstOrDefaultAsync(
                execution => execution.SourceConnectorId == sourceConnectorId && execution.SourceEventId == sourceEventId,
                cancellationToken);

    private static bool IsUniqueConstraintViolation(DbUpdateException exception) =>
        exception.InnerException is SqlException { Number: 2601 or 2627 };
}
