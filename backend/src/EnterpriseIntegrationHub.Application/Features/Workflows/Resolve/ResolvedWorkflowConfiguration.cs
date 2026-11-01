using EnterpriseIntegrationHub.Domain.Enums;

namespace EnterpriseIntegrationHub.Application.Features.Workflows.Resolve;

public sealed record ResolvedWorkflowConfiguration(
    Guid WorkflowRevisionId,
    Guid WorkflowId,
    int ConfigurationRevision,
    string WorkflowName,
    Guid SourceConnectorId,
    string TriggerEvent,
    IReadOnlyCollection<ResolvedWorkflowStep> Steps);

public sealed record ResolvedWorkflowStep(
    Guid WorkflowRevisionStepId,
    Guid DestinationConnectorId,
    string BaseUrl,
    ConnectorProtocol Protocol,
    ConnectorAuthenticationType AuthenticationType,
    int TimeoutSeconds,
    int ExecutionOrder);
