using EnterpriseIntegrationHub.Application.Interfaces;
using EnterpriseIntegrationHub.Domain.Enums;

namespace EnterpriseIntegrationHub.Application.Features.Connectors.Activate;

public sealed class ActivateConnectorHandler
{
  private readonly IConnectorRepository _repository;
  private readonly IExternalSystemRepository _externalSystemRepository;

  public ActivateConnectorHandler(
      IConnectorRepository repository,
      IExternalSystemRepository externalSystemRepository)
  {
    _repository = repository;
    _externalSystemRepository = externalSystemRepository;
  }

  public async Task Handle(ActivateConnectorCommand command, CancellationToken cancellationToken)
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

    if (connector.Status != ConnectorStatus.Draft)
    {
      throw new InvalidOperationException($"Connector with ID {connector.Id} must be in Draft status to activate.");
    }

    var externalSystem = await _externalSystemRepository.GetByIdAsync(connector.ExternalSystemId, cancellationToken);

    if (externalSystem is null)
    {
      throw new KeyNotFoundException($"External system with ID {connector.ExternalSystemId} not found.");
    }

    if (externalSystem.Status != ExternalSystemStatus.Active)
    {
      throw new InvalidOperationException($"External system with ID {connector.ExternalSystemId} is not active.");
    }

    connector.Activate();
    await _repository.UpdateAsync(connector, cancellationToken);
  }
}
