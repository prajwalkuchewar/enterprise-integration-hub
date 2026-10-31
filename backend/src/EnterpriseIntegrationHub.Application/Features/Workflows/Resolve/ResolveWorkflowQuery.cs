namespace EnterpriseIntegrationHub.Application.Features.Workflows.Resolve;

public sealed record ResolveWorkflowQuery(Guid SourceConnectorId, string TriggerEvent);
