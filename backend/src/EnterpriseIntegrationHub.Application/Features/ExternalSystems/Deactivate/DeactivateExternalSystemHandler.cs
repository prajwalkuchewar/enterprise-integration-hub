using EnterpriseIntegrationHub.Application.Interfaces;

namespace EnterpriseIntegrationHub.Application.Features.ExternalSystems.Deactivate;

public sealed class DeactivateExternalSystemHandler(
    IExternalSystemRepository repository,
    IConnectorRepository connectorRepository,
    IWorkflowRepository workflowRepository)
{
    public async Task Handle(DeactivateExternalSystemCommand command, CancellationToken cancellationToken)
    {
        var externalSystem = await repository.GetByIdAsync(command.ExternalSystemId, cancellationToken)
            ?? throw new KeyNotFoundException($"External system with ID {command.ExternalSystemId} not found.");

        if (await connectorRepository.HasActiveConnectorForExternalSystemAsync(externalSystem.Id, cancellationToken))
            throw new InvalidOperationException($"External system with ID {externalSystem.Id} cannot be deactivated because it has active connectors.");

        if (await workflowRepository.HasActiveWorkflowForExternalSystemAsync(externalSystem.Id, cancellationToken))
            throw new InvalidOperationException($"External system with ID {externalSystem.Id} cannot be deactivated because an active workflow depends on it.");

        externalSystem.Deactivate();
        await repository.UpdateAsync(externalSystem, cancellationToken);
    }
}
