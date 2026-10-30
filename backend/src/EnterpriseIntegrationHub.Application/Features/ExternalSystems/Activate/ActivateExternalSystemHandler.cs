using EnterpriseIntegrationHub.Application.Interfaces;

namespace EnterpriseIntegrationHub.Application.Features.ExternalSystems.Activate;

public sealed class ActivateExternalSystemHandler(IExternalSystemRepository repository)
{
    public async Task Handle(ActivateExternalSystemCommand command, CancellationToken cancellationToken)
    {
        var externalSystem = await repository.GetByIdAsync(command.ExternalSystemId, cancellationToken)
            ?? throw new KeyNotFoundException($"External system with ID {command.ExternalSystemId} not found.");
        externalSystem.Activate();
        await repository.UpdateAsync(externalSystem, cancellationToken);
    }
}
