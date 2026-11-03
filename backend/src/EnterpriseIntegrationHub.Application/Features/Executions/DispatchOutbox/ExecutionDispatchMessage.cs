namespace EnterpriseIntegrationHub.Application.Features.Executions.DispatchOutbox;

public sealed record ExecutionDispatchMessage(
    Guid DispatchId,
    int Attempt,
    Guid ExecutionId,
    Guid WorkflowId,
    int WorkflowRevisionNumber,
    Guid SourceConnectorId,
    string SourceEventId,
    string TriggerEvent,
    DateTimeOffset OccurredAt,
    string? CorrelationId);
