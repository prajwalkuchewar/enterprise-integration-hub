namespace EnterpriseIntegrationHub.Application.Features.Connectors.IssueIngestionKey;

/// <summary>Returned once at issuance. The plaintext key cannot be recovered later.</summary>
public sealed record IssueConnectorIngestionKeyResult(Guid ConnectorId, string IngestionKey);
