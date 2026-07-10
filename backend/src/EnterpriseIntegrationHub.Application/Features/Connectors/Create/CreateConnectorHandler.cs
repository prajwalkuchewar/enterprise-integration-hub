using EnterpriseIntegrationHub.Application.Interfaces;
using EnterpriseIntegrationHub.Domain.Entities;
using EnterpriseIntegrationHub.Domain.Enums;

namespace EnterpriseIntegrationHub.Application.Features.Connectors.Create;

public sealed class CreateConnectorHandler
{
    private readonly IConnectorRepository _repository;
    private readonly IExternalSystemRepository _externalSystemRepository;

    public CreateConnectorHandler(IConnectorRepository repository, IExternalSystemRepository externalSystemRepository)
    {
        _repository = repository;
        _externalSystemRepository = externalSystemRepository;
    }

    public async Task<Guid> Handle(
        CreateConnectorCommand command,
        CancellationToken cancellationToken)
    {

        // Validate command early to protect application/business rules
        var validator = new CreateConnectorCommandValidator();
        validator.Validate(command);


        var externalSystem = await _externalSystemRepository
        .GetByIdAsync(command.ExternalSystemId, cancellationToken);

        if (externalSystem == null)
        {
            throw new KeyNotFoundException(
                $"External system with ID {command.ExternalSystemId} not found.");
        }

        if(externalSystem.Status != ExternalSystemStatus.Active)
        {
            throw new InvalidOperationException(
                $"External system with ID {command.ExternalSystemId} is not active.");
        }

        var exists = await _repository.ExistsAsync(
            command.ExternalSystemId,
            command.Name,
            cancellationToken);

        if (exists)
        {
            throw new InvalidOperationException($"Connector with name '{command.Name}' and External System '{command.ExternalSystemId}' already exists.");
        }

        var connector = new Connector(
            name: command.Name,
            description: command.Description,
            externalSystemId: command.ExternalSystemId,
            baseUrl: command.BaseUrl,
            protocol: command.Protocol,
            authenticationType: command.AuthenticationType,
            timeoutSeconds: command.TimeoutSeconds,
            status: ConnectorStatus.Draft);

        await _repository.AddAsync(connector, cancellationToken);

        return connector.Id;
    }
}
