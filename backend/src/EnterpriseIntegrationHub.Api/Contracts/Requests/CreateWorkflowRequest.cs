using System.ComponentModel.DataAnnotations;

namespace EnterpriseIntegrationHub.Api.Contracts.Requests;

/// <summary>Request payload for creating a workflow.</summary>
public sealed class CreateWorkflowRequest
{
    /// <summary>Gets the workflow name.</summary>
    [Required, MaxLength(200)]
    public string Name { get; init; } = string.Empty;

    /// <summary>Gets the source connector identifier.</summary>
    public Guid SourceConnectorId { get; init; }

    /// <summary>Gets the ordered workflow steps.</summary>
    [Required, MinLength(1)]
    public IReadOnlyCollection<CreateWorkflowStepRequest> Steps { get; init; } = [];

    /// <summary>Gets the trigger event name.</summary>
    [Required, MaxLength(200)]
    public string TriggerEvent { get; init; } = string.Empty;
}
