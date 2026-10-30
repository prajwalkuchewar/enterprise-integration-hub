using EnterpriseIntegrationHub.Application.Interfaces;

namespace EnterpriseIntegrationHub.Application.Features.Connectors.Deactivate;

public sealed class DeactivateConnectorHandler(IConnectorRepository repository)
{
    public async Task Handle(DeactivateConnectorCommand command, CancellationToken cancellationToken)
    {
        var connector = await repository.GetByIdAsync(command.ConnectorId, cancellationToken)
            ?? throw new KeyNotFoundException($"Connector with ID {command.ConnectorId} not found.");
        connector.Deactivate();
        await repository.UpdateAsync(connector, cancellationToken);
    }
}
