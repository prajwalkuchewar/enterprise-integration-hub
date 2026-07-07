using EnterpriseIntegrationHub.Application.Interfaces;
using EnterpriseIntegrationHub.Domain.Entities;
using EnterpriseIntegrationHub.Domain.Enums;

namespace EnterpriseIntegrationHub.Application.Features.ExternalSystems.Create;

public sealed class CreateExternalSystemHandler
{
    private readonly IExternalSystemRepository _repository;

    public CreateExternalSystemHandler(IExternalSystemRepository repository)
    {
        _repository = repository;
    }

    public async Task<Guid> Handle(
        CreateExternalSystemCommand command,
        CancellationToken cancellationToken)
    {
        var exists = await _repository.ExistsAsync(
            command.Name,
            command.Environment,
            cancellationToken);

        if (exists)
        {
            throw new InvalidOperationException($"External system with name '{command.Name}' and environment '{command.Environment}' already exists.");
        }

        var externalSystem = new ExternalSystem(
            name: command.Name,
            description: command.Description,
            environment: command.Environment,
            status: ExternalSystemStatus.Active);

        await _repository.AddAsync(externalSystem, cancellationToken);

        return externalSystem.Id;
    }
}
