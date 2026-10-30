using EnterpriseIntegrationHub.Application.Interfaces;

namespace EnterpriseIntegrationHub.Application.Features.ExternalSystems.Deactivate;

public sealed class DeactivateExternalSystemHandler(IExternalSystemRepository repository)
{
    public async Task Handle(DeactivateExternalSystemCommand command, CancellationToken cancellationToken)
    {
        var externalSystem = await repository.GetByIdAsync(command.ExternalSystemId, cancellationToken)
            ?? throw new KeyNotFoundException($"External system with ID {command.ExternalSystemId} not found.");
        externalSystem.Deactivate();
        await repository.UpdateAsync(externalSystem, cancellationToken);
    }
}
