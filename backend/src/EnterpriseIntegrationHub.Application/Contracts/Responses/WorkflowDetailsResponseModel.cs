using EnterpriseIntegrationHub.Domain.Enums;

namespace EnterpriseIntegrationHub.Application.Contracts.Responses;

public sealed record WorkflowDetailsResponseModel(
    Guid Id,
    string Name,
    Guid SourceConnectorId,
    string TriggerEvent,
    WorkflowStatus Status,
    IReadOnlyCollection<WorkflowStepResponseModel> Steps);

public sealed record WorkflowStepResponseModel(Guid DestinationConnectorId, int ExecutionOrder);
