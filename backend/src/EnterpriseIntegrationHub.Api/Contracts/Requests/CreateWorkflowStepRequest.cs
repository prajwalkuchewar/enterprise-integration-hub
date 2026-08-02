using System.ComponentModel.DataAnnotations;

namespace EnterpriseIntegrationHub.Api.Contracts.Requests;

/// <summary>
/// Defines one ordered destination action in a workflow creation request.
/// </summary>
public sealed class CreateWorkflowStepRequest
{
    /// <summary>The connector that receives this step's routed event.</summary>
    public Guid DestinationConnectorId { get; init; }

    /// <summary>The one-based position of this step within the workflow.</summary>
    [Range(1, int.MaxValue)]
    public int ExecutionOrder { get; init; }
}
