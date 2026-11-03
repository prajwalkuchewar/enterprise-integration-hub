namespace EnterpriseIntegrationHub.Application.Interfaces;

using EnterpriseIntegrationHub.Application.Features.Executions.DispatchOutbox;

public interface IExecutionDispatchPublisher
{
    Task PublishAsync(ExecutionDispatchMessage message, CancellationToken cancellationToken);
}
