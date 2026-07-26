using EnterpriseIntegrationHub.Application.Interfaces;
using EnterpriseIntegrationHub.Domain.Entities;
using EnterpriseIntegrationHub.Domain.Enums;

namespace EnterpriseIntegrationHub.Application.Features.Workflows.Create;

public sealed class CreateWorkflowHandler
{
    private readonly IWorkflowRepository _workflowRepository;
    private readonly IConnectorRepository _connectorRepository;

    public CreateWorkflowHandler(IWorkflowRepository workflowRepository, IConnectorRepository connectorRepository)
    {
        _workflowRepository = workflowRepository;
        _connectorRepository = connectorRepository;
    }

    public async Task<Guid> Handle(CreateWorkflowCommand command, CancellationToken cancellationToken)
    {
        var workflow = new Workflow(command.Name, command.SourceConnectorId, command.TriggerEvent,
            command.Steps.Select(x => new WorkflowStepDefinition(x.DestinationConnectorId, x.ExecutionOrder)));

        if (await _workflowRepository.ExistsByNameAsync(command.Name, cancellationToken))
            throw new InvalidOperationException($"Workflow with name '{command.Name}' already exists.");

        await EnsureActiveConnector(command.SourceConnectorId, "Source", cancellationToken);
        foreach (var destinationId in workflow.Steps.Select(x => x.DestinationConnectorId))
            await EnsureActiveConnector(destinationId, "Destination", cancellationToken);

        await _workflowRepository.AddAsync(workflow, cancellationToken);
        return workflow.Id;
    }

    private async Task EnsureActiveConnector(Guid connectorId, string role, CancellationToken cancellationToken)
    {
        var connector = await _connectorRepository.GetByIdAsync(connectorId, cancellationToken);
        if (connector is null)
            throw new KeyNotFoundException($"{role} connector with ID {connectorId} not found.");
        if (connector.Status != ConnectorStatus.Active)
            throw new InvalidOperationException($"{role} connector with ID {connectorId} is not active.");
    }
}
