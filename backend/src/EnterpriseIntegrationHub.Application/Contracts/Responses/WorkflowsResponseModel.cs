using EnterpriseIntegrationHub.Domain.Enums;

namespace EnterpriseIntegrationHub.Application.Contracts.Responses;

public sealed record WorkflowsResponseModel(IReadOnlyCollection<WorkflowSummary> Items, int TotalCount);

public sealed record WorkflowSummary(
    Guid Id,
    string Name,
    Guid SourceConnectorId,
    string TriggerEvent,
    WorkflowStatus Status,
    int StepCount);
