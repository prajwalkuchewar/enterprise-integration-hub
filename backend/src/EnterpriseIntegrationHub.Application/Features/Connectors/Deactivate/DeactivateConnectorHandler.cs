using EnterpriseIntegrationHub.Application.Interfaces;

namespace EnterpriseIntegrationHub.Application.Features.Connectors.Deactivate;

public sealed class DeactivateConnectorHandler(IConnectorRepository repository, IWorkflowRepository workflowRepository)
{
    public async Task Handle(DeactivateConnectorCommand command, CancellationToken cancellationToken)
    {
        var connector = await repository.GetByIdAsync(command.ConnectorId, cancellationToken)
            ?? throw new KeyNotFoundException($"Connector with ID {command.ConnectorId} not found.");

        if (await workflowRepository.HasActiveWorkflowForConnectorAsync(connector.Id, cancellationToken))
            throw new InvalidOperationException($"Connector with ID {connector.Id} cannot be deactivated because an active workflow depends on it.");

        connector.Deactivate();
        await repository.UpdateAsync(connector, cancellationToken);
    }
}
