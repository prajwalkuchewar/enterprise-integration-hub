using EnterpriseIntegrationHub.Application.Interfaces;
using EnterpriseIntegrationHub.Domain.Enums;

namespace EnterpriseIntegrationHub.Application.Features.Workflows.Activate;

public sealed class ActivateWorkflowHandler(IWorkflowRepository workflowRepository, IConnectorRepository connectorRepository)
{
    public async Task Handle(ActivateWorkflowCommand command, CancellationToken cancellationToken)
    {
        if (command.WorkflowId == Guid.Empty)
            throw new ArgumentException("WorkflowId must be provided.", nameof(command.WorkflowId));

        var workflow = await workflowRepository.GetByIdAsync(command.WorkflowId, cancellationToken)
            ?? throw new KeyNotFoundException($"Workflow with ID {command.WorkflowId} not found.");

        await EnsureActiveConnector(workflow.SourceConnectorId, "Source", cancellationToken);
        foreach (var step in workflow.Steps)
            await EnsureActiveConnector(step.DestinationConnectorId, "Destination", cancellationToken);

        workflow.Activate();
        await workflowRepository.UpdateAsync(workflow, cancellationToken);
    }

    private async Task EnsureActiveConnector(Guid connectorId, string role, CancellationToken cancellationToken)
    {
        var connector = await connectorRepository.GetByIdAsync(connectorId, cancellationToken)
            ?? throw new KeyNotFoundException($"{role} connector with ID {connectorId} not found.");
        if (connector.Status != ConnectorStatus.Active)
            throw new InvalidOperationException($"{role} connector with ID {connectorId} is not active.");
    }
}
