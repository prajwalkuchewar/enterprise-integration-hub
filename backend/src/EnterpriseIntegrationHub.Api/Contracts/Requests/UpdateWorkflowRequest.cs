using System.ComponentModel.DataAnnotations;

namespace EnterpriseIntegrationHub.Api.Contracts.Requests;

/// <summary>Request payload for updating a draft workflow.</summary>
public sealed class UpdateWorkflowRequest
{
    /// <summary>Gets the workflow name.</summary>
    [Required, MaxLength(200)]
    public string Name { get; init; } = string.Empty;

    /// <summary>Gets the source connector identifier.</summary>
    public Guid SourceConnectorId { get; init; }

    /// <summary>Gets the ordered workflow steps.</summary>
    [Required, MinLength(1)]
    public IReadOnlyCollection<UpdateWorkflowStepRequest> Steps { get; init; } = [];

    /// <summary>Gets the trigger event name.</summary>
    [Required, MaxLength(200)]
    public string TriggerEvent { get; init; } = string.Empty;
}

/// <summary>Request payload for one workflow step update.</summary>
public sealed class UpdateWorkflowStepRequest
{
    /// <summary>Gets the destination connector identifier.</summary>
    public Guid DestinationConnectorId { get; init; }

    /// <summary>Gets the execution order.</summary>
    [Range(1, int.MaxValue)] public int ExecutionOrder { get; init; }
}
