using EnterpriseIntegrationHub.Application.Features.Executions.DispatchOutbox;
using EnterpriseIntegrationHub.Domain.Entities;
using EnterpriseIntegrationHub.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseIntegrationHub.Infrastructure.Messaging.Kafka;

public sealed class ExecutionDispatchInboxWriter(EnterpriseIntegrationHubDbContext context) : IExecutionDispatchInboxWriter
{
    public async Task StoreAsync(ExecutionDispatchMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);

        var stored = await (from dispatch in context.ExecutionDispatchOutbox.AsNoTracking()
                            join execution in context.IntegrationExecutions.AsNoTracking()
                                on dispatch.IntegrationExecutionId equals execution.Id
                            where dispatch.Id == message.DispatchId
                            select new
                            {
                                execution.Id,
                                execution.WorkflowId,
                                execution.WorkflowRevisionNumber,
                                execution.SourceConnectorId,
                                execution.SourceEventId,
                                execution.TriggerEvent,
                                execution.OccurredAt,
                                execution.CorrelationId
                            }).SingleOrDefaultAsync(cancellationToken);

        if (stored is null || stored.Id != message.ExecutionId ||
            stored.WorkflowId != message.WorkflowId ||
            stored.WorkflowRevisionNumber != message.WorkflowRevisionNumber ||
            stored.SourceConnectorId != message.SourceConnectorId ||
            stored.SourceEventId != message.SourceEventId ||
            stored.TriggerEvent != message.TriggerEvent ||
            stored.OccurredAt != message.OccurredAt ||
            stored.CorrelationId != message.CorrelationId)
            throw new InvalidDataException("Dispatch does not match the stored execution.");

        if (await context.ExecutionDispatchInbox.AsNoTracking()
            .AnyAsync(item => item.DispatchId == message.DispatchId, cancellationToken))
            return;

        var item = new ExecutionDispatchInbox(message.DispatchId, message.ExecutionId, DateTimeOffset.UtcNow);
        context.ExecutionDispatchInbox.Add(item);
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            context.Entry(item).State = EntityState.Detached;
            if (!await context.ExecutionDispatchInbox.AsNoTracking().AnyAsync(
                    existing => existing.DispatchId == message.DispatchId &&
                                existing.IntegrationExecutionId == message.ExecutionId, cancellationToken))
                throw;
        }
    }
}
