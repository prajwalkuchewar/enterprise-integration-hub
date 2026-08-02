using System.ComponentModel.DataAnnotations;

namespace EnterpriseIntegrationHub.Api.Contracts.Requests;

public sealed class UpdateWorkflowRequest
{
    [Required, MaxLength(200)] public string Name { get; init; } = string.Empty;
    public Guid SourceConnectorId { get; init; }
    [Required, MinLength(1)] public IReadOnlyCollection<UpdateWorkflowStepRequest> Steps { get; init; } = [];
    [Required, MaxLength(200)] public string TriggerEvent { get; init; } = string.Empty;
}

public sealed class UpdateWorkflowStepRequest
{
    public Guid DestinationConnectorId { get; init; }
    [Range(1, int.MaxValue)] public int ExecutionOrder { get; init; }
}
