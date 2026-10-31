namespace EnterpriseIntegrationHub.Application.Features.Workflows.Resolve;

public sealed record ResolvedWorkflowConfiguration(
    Guid WorkflowId,
    int ConfigurationRevision,
    Guid SourceConnectorId,
    string TriggerEvent,
    IReadOnlyCollection<ResolvedWorkflowStep> Steps);

public sealed record ResolvedWorkflowStep(Guid DestinationConnectorId, int ExecutionOrder);
