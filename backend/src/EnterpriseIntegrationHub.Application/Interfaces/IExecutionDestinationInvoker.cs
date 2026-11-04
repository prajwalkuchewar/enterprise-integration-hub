using EnterpriseIntegrationHub.Application.Features.Executions.Worker;

namespace EnterpriseIntegrationHub.Application.Interfaces;

public interface IExecutionDestinationInvoker
{
    Task<DestinationInvocationResult> InvokeAsync(ExecutionAttemptWork work, CancellationToken cancellationToken);
}
