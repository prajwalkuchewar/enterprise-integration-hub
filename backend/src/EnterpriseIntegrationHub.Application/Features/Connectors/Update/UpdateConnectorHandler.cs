using EnterpriseIntegrationHub.Application.Interfaces;
using EnterpriseIntegrationHub.Domain.Enums;

namespace EnterpriseIntegrationHub.Application.Features.Connectors.Update;

public sealed class UpdateConnectorHandler
{
    private readonly IConnectorRepository _repository;
    private readonly IExternalSystemRepository _externalSystemRepository;

    public UpdateConnectorHandler(IConnectorRepository repository, IExternalSystemRepository externalSystemRepository)
    {
        _repository = repository;
        _externalSystemRepository = externalSystemRepository;
    }

    public async Task Handle(Guid id, UpdateConnectorCommand command, CancellationToken cancellationToken)
    {
        var validator = new UpdateConnectorCommandValidator();
        validator.Validate(command);

        var connector = await _repository.GetByIdAsync(id, cancellationToken);
        if (connector == null)
            throw new KeyNotFoundException($"Connector with ID {id} not found.");

        var externalSystem = await _externalSystemRepository.GetByIdAsync(connector.ExternalSystemId, cancellationToken);
        if (externalSystem == null)
            throw new KeyNotFoundException($"External system with ID {connector.ExternalSystemId} not found.");

        if (externalSystem.Status != ExternalSystemStatus.Active)
            throw new InvalidOperationException($"External system with ID {connector.ExternalSystemId} is not active.");

        // Ensure name remains unique within the same external system (excluding this connector)
        var nameConflict = (await _repository.GetAllAsync(cancellationToken))
            .Any(x => x.ExternalSystemId == connector.ExternalSystemId && x.Name == command.Name && x.Id != connector.Id);

        if (nameConflict)
            throw new InvalidOperationException($"Connector with name '{command.Name}' and External System '{connector.ExternalSystemId}' already exists.");

        // Apply updates via domain methods
        connector.UpdateBasicInfo(command.Name, command.Description);
        connector.UpdateCommunication(command.BaseUrl, command.Protocol, command.AuthenticationType, command.TimeoutSeconds);

        await _repository.UpdateAsync(connector, cancellationToken);
    }
}
