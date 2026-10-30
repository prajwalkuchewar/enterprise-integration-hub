using EnterpriseIntegrationHub.Application.Interfaces;

namespace EnterpriseIntegrationHub.Application.Features.ExternalSystems.Update;

public sealed class UpdateExternalSystemHandler(IExternalSystemRepository repository)
{
    public async Task Handle(Guid id, UpdateExternalSystemCommand command, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("External system id must be provided.", nameof(id));

        if (string.IsNullOrWhiteSpace(command.Name))
            throw new ArgumentException("Name is required.", nameof(command.Name));

        if (string.IsNullOrWhiteSpace(command.Description))
            throw new ArgumentException("Description is required.", nameof(command.Description));

        var externalSystem = await repository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"External system with ID {id} not found.");

        var duplicate = await repository.ExistsAsync(command.Name, command.Environment, cancellationToken);
        if (duplicate && (externalSystem.Name != command.Name || externalSystem.Environment != command.Environment))
            throw new InvalidOperationException($"External system with name '{command.Name}' and environment '{command.Environment}' already exists.");

        externalSystem.Update(command.Name, command.Description, command.Environment);
        await repository.UpdateAsync(externalSystem, cancellationToken);
    }
}
