using EnterpriseIntegrationHub.Domain.Common;
using EnterpriseIntegrationHub.Domain.Enums;

namespace EnterpriseIntegrationHub.Domain.Entities;

public sealed class WorkflowRevision : BaseEntity
{
    private WorkflowRevision() { }

    public WorkflowRevision(Workflow workflow)
    {
        ArgumentNullException.ThrowIfNull(workflow);
        if (workflow.Status != WorkflowStatus.Active)
            throw new InvalidOperationException("Only an active workflow can be published as a revision.");

        WorkflowId = workflow.Id;
        RevisionNumber = workflow.ConfigurationRevision;
        Name = workflow.Name;
        SourceConnectorId = workflow.SourceConnectorId;
        TriggerEvent = workflow.TriggerEvent;
        PublishedAt = DateTimeOffset.UtcNow;
        Steps = workflow.Steps
            .OrderBy(step => step.ExecutionOrder)
            .Select(step => new WorkflowRevisionStep(Id, step.DestinationConnectorId, step.ExecutionOrder))
            .ToList();
    }

    public Guid WorkflowId { get; private set; }
    public int RevisionNumber { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public Guid SourceConnectorId { get; private set; }
    public string TriggerEvent { get; private set; } = string.Empty;
    public DateTimeOffset PublishedAt { get; private set; }
    public ICollection<WorkflowRevisionStep> Steps { get; private set; } = new List<WorkflowRevisionStep>();
}
