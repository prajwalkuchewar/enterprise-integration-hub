using EnterpriseIntegrationHub.Application.Features.Executions.DispatchOutbox;

namespace EnterpriseIntegrationHub.Infrastructure.Messaging.Kafka;

public interface IExecutionDispatchInboxWriter
{
    Task StoreAsync(ExecutionDispatchMessage message, CancellationToken cancellationToken);
}
