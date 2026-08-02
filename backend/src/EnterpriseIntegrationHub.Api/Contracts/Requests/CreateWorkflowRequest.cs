using System.ComponentModel.DataAnnotations;

namespace EnterpriseIntegrationHub.Api.Contracts.Requests;

public sealed class CreateWorkflowRequest
{
    [Required, MaxLength(200)]
    public string Name { get; init; } = string.Empty;

    public Guid SourceConnectorId { get; init; }

    [Required, MinLength(1)]
    public IReadOnlyCollection<CreateWorkflowStepRequest> Steps { get; init; } = [];

    [Required, MaxLength(200)]
    public string TriggerEvent { get; init; } = string.Empty;
}
