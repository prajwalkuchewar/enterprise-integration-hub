using EnterpriseIntegrationHub.Application.Interfaces;
using EnterpriseIntegrationHub.Application.Security;

namespace EnterpriseIntegrationHub.Application.Features.Connectors.IssueIngestionKey;

public sealed class IssueConnectorIngestionKeyHandler(IConnectorRepository connectorRepository)
{
    public async Task<IssueConnectorIngestionKeyResult> Handle(Guid connectorId, CancellationToken cancellationToken)
    {
        if (connectorId == Guid.Empty)
            throw new ArgumentException("ConnectorId must be provided.", nameof(connectorId));

        var connector = await connectorRepository.GetByIdAsync(connectorId, cancellationToken)
            ?? throw new KeyNotFoundException($"Connector with ID {connectorId} not found.");

        // Only the hash is persisted. Reissuing replaces the previous key, which stops working immediately.
        var ingestionKey = IngestionKeys.Generate();
        connector.AssignIngestionKeyHash(IngestionKeys.Hash(ingestionKey));
        await connectorRepository.UpdateAsync(connector, cancellationToken);

        return new IssueConnectorIngestionKeyResult(connector.Id, ingestionKey);
    }
}
