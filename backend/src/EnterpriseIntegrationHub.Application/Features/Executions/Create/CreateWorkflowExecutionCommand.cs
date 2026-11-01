namespace EnterpriseIntegrationHub.Application.Features.Executions.Create;

public sealed record CreateWorkflowExecutionCommand(
    Guid SourceConnectorId,
    string SourceEventId,
    string TriggerEvent,
    DateTimeOffset OccurredAt,
    string? CorrelationId,
    string EventFingerprint);
