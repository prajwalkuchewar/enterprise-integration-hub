using EnterpriseIntegrationHub.Domain.Common;
using EnterpriseIntegrationHub.Domain.Enums;

namespace EnterpriseIntegrationHub.Domain.Entities;

public sealed class Workflow : BaseEntity
{
    private readonly List<WorkflowStep> _steps = [];

    public string Name { get; private set; } = string.Empty;
    public Guid SourceConnectorId { get; private set; }
    public string TriggerEvent { get; private set; } = string.Empty;
    public WorkflowStatus Status { get; private set; }
    public IReadOnlyCollection<WorkflowStep> Steps => _steps.AsReadOnly();

    private Workflow() { }

    public Workflow(string name, Guid sourceConnectorId, string triggerEvent, IEnumerable<WorkflowStepDefinition> steps)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Workflow name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(triggerEvent))
            throw new ArgumentException("Trigger event is required.", nameof(triggerEvent));

        var stepDefinitions = steps.ToList();
        if (stepDefinitions.Count == 0)
            throw new ArgumentException("At least one workflow step is required.", nameof(steps));
        if (stepDefinitions.Any(x => x.ExecutionOrder <= 0))
            throw new ArgumentException("Workflow step execution order must be positive.", nameof(steps));
        if (stepDefinitions.GroupBy(x => x.ExecutionOrder).Any(x => x.Count() > 1))
            throw new ArgumentException("Workflow step execution orders must be unique.", nameof(steps));
        if (stepDefinitions.Any(x => x.DestinationConnectorId == sourceConnectorId))
            throw new ArgumentException("Source and destination connectors cannot be the same.", nameof(steps));

        Name = name;
        SourceConnectorId = sourceConnectorId;
        TriggerEvent = triggerEvent;
        Status = WorkflowStatus.Draft;
        _steps.AddRange(stepDefinitions.Select(x => new WorkflowStep(Id, x.DestinationConnectorId, x.ExecutionOrder)));
    }

    public void Activate()
    {
        if (Status != WorkflowStatus.Draft)
            throw new InvalidOperationException($"Workflow with ID {Id} must be in Draft status to activate.");

        Status = WorkflowStatus.Active;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
