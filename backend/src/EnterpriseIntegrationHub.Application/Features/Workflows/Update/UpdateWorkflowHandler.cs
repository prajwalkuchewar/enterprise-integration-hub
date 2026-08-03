using EnterpriseIntegrationHub.Application.Interfaces;
using EnterpriseIntegrationHub.Domain.Entities;
using EnterpriseIntegrationHub.Domain.Enums;

namespace EnterpriseIntegrationHub.Application.Features.Workflows.Update;

public sealed class UpdateWorkflowHandler(IWorkflowRepository workflowRepository, IConnectorRepository connectorRepository)
{
    public async Task Handle(Guid id, UpdateWorkflowCommand command, CancellationToken cancellationToken)
    {
        var workflow = await workflowRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Workflow with ID {id} not found.");
        if (await workflowRepository.ExistsByNameAsync(command.Name, cancellationToken, id))
            throw new InvalidOperationException($"Workflow with name '{command.Name}' already exists.");

        await EnsureActiveConnector(command.SourceConnectorId, "Source", cancellationToken);
        foreach (var step in command.Steps)
            await EnsureActiveConnector(step.DestinationConnectorId, "Destination", cancellationToken);

        workflow.Update(command.Name, command.SourceConnectorId, command.TriggerEvent,
            command.Steps.Select(x => new WorkflowStepDefinition(x.DestinationConnectorId, x.ExecutionOrder)));
        await workflowRepository.UpdateAsync(workflow, cancellationToken);
    }

    private async Task EnsureActiveConnector(Guid id, string role, CancellationToken cancellationToken)
    {
        var connector = await connectorRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"{role} connector with ID {id} not found.");
        if (connector.Status != ConnectorStatus.Active)
            throw new InvalidOperationException($"{role} connector with ID {id} is not active.");
    }
}
