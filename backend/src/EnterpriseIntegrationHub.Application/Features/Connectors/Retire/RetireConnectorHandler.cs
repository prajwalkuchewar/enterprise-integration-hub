using EnterpriseIntegrationHub.Application.Interfaces;

namespace EnterpriseIntegrationHub.Application.Features.Connectors.Retire;

public sealed class RetireConnectorHandler
{
  private readonly IConnectorRepository _repository;

  public RetireConnectorHandler( IConnectorRepository repository)
  {
    _repository = repository;
  }

  public async Task Handle(RetireConnectorCommand command, CancellationToken cancellationToken)
  {
    if (command.ConnectorId == Guid.Empty)
    {
      throw new ArgumentException("ConnectorId must be provided.", nameof(command.ConnectorId));
    }

    var connector = await _repository.GetByIdAsync(command.ConnectorId, cancellationToken);

    if (connector is null)
    {
      throw new KeyNotFoundException($"Connector with ID {command.ConnectorId} not found.");
    }

    //check for active workflows associated with the connector

    connector.Retire();
    await _repository.UpdateAsync(connector, cancellationToken);
  }
}
