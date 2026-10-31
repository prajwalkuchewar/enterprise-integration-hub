using EnterpriseIntegrationHub.Domain.Common;
using EnterpriseIntegrationHub.Domain.Enums;

namespace EnterpriseIntegrationHub.Domain.Entities;

public sealed class IntegrationExecution : BaseEntity
{
    private IntegrationExecution() { }

    public IntegrationExecution(
        WorkflowRevision workflowRevision,
        Guid sourceConnectorId,
        string sourceEventId,
        DateTimeOffset occurredAt,
        string? correlationId,
        string eventFingerprint,
        IEnumerable<ExecutionStepSnapshot> steps)
    {
        ArgumentNullException.ThrowIfNull(workflowRevision);
        ArgumentNullException.ThrowIfNull(steps);
        if (sourceConnectorId == Guid.Empty)
            throw new ArgumentException("SourceConnectorId must be provided.", nameof(sourceConnectorId));
        if (sourceConnectorId != workflowRevision.SourceConnectorId)
            throw new ArgumentException("The event source must match the workflow revision source connector.", nameof(sourceConnectorId));
        if (string.IsNullOrWhiteSpace(sourceEventId))
            throw new ArgumentException("SourceEventId is required.", nameof(sourceEventId));
        if (sourceEventId.Trim().Length > 200)
            throw new ArgumentException("SourceEventId cannot exceed 200 characters.", nameof(sourceEventId));
        if (workflowRevision.TriggerEvent.Length > 200)
            throw new ArgumentException("TriggerEvent cannot exceed 200 characters.", nameof(workflowRevision));
        if (string.IsNullOrWhiteSpace(eventFingerprint))
            throw new ArgumentException("EventFingerprint is required.", nameof(eventFingerprint));
        if (eventFingerprint.Length != 64 || eventFingerprint.Any(character => !Uri.IsHexDigit(character)))
            throw new ArgumentException("EventFingerprint must be a 64-character SHA-256 hex value.", nameof(eventFingerprint));
        if (correlationId?.Trim().Length > 200)
            throw new ArgumentException("CorrelationId cannot exceed 200 characters.", nameof(correlationId));

        WorkflowId = workflowRevision.WorkflowId;
        WorkflowRevisionId = workflowRevision.Id;
        WorkflowRevisionNumber = workflowRevision.RevisionNumber;
        SourceConnectorId = sourceConnectorId;
        SourceEventId = sourceEventId.Trim();
        TriggerEvent = workflowRevision.TriggerEvent;
        OccurredAt = occurredAt;
        ReceivedAt = DateTimeOffset.UtcNow;
        CorrelationId = string.IsNullOrWhiteSpace(correlationId) ? null : correlationId.Trim();
        EventFingerprint = eventFingerprint.Trim();
        Status = IntegrationExecutionStatus.Queued;

        var snapshots = steps.OrderBy(step => step.ExecutionOrder).ToArray();
        if (snapshots.Length != workflowRevision.Steps.Count)
            throw new ArgumentException("The execution must snapshot every step in its workflow revision.", nameof(steps));
        if (snapshots.Length == 0)
            throw new ArgumentException("At least one execution step is required.", nameof(steps));
        if (snapshots.Select(step => step.ExecutionOrder).Distinct().Count() != snapshots.Length)
            throw new ArgumentException("Execution step orders must be unique.", nameof(steps));

        var expectedSteps = workflowRevision.Steps.ToDictionary(step => step.ExecutionOrder);
        if (snapshots.Any(snapshot => !expectedSteps.TryGetValue(snapshot.ExecutionOrder, out var revisionStep) ||
            snapshot.WorkflowRevisionStepId != revisionStep.Id || snapshot.DestinationConnectorId != revisionStep.DestinationConnectorId))
            throw new ArgumentException("Execution step snapshots must match their workflow revision.", nameof(steps));

        Steps = snapshots.Select(snapshot => new ExecutionStep(Id, snapshot)).ToList();
    }

    public Guid WorkflowId { get; private set; }
    public Guid WorkflowRevisionId { get; private set; }
    public int WorkflowRevisionNumber { get; private set; }
    public Guid SourceConnectorId { get; private set; }
    public string SourceEventId { get; private set; } = string.Empty;
    public string TriggerEvent { get; private set; } = string.Empty;
    public DateTimeOffset OccurredAt { get; private set; }
    public DateTimeOffset ReceivedAt { get; private set; }
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public string? CorrelationId { get; private set; }
    public string EventFingerprint { get; private set; } = string.Empty;
    public IntegrationExecutionStatus Status { get; private set; }
    public ICollection<ExecutionStep> Steps { get; private set; } = new List<ExecutionStep>();

    public void Start()
    {
        if (Status is not (IntegrationExecutionStatus.Queued or IntegrationExecutionStatus.RetryScheduled))
            throw new InvalidOperationException("Only a queued or retry-scheduled execution can start.");
        Status = IntegrationExecutionStatus.Running;
        StartedAt ??= DateTimeOffset.UtcNow;
    }

    public void MarkStepSucceeded(int executionOrder)
    {
        EnsureRunning();
        var step = GetCurrentStep(executionOrder);
        step.MarkSucceeded();
        if (Steps.All(candidate => candidate.Status == ExecutionStepStatus.Succeeded))
        {
            Status = IntegrationExecutionStatus.Succeeded;
            CompletedAt = DateTimeOffset.UtcNow;
        }
    }

    public void MarkStepFailed(int executionOrder, string failureCode, string failureMessage, DateTimeOffset? retryAt)
    {
        EnsureRunning();
        var step = GetCurrentStep(executionOrder);
        step.MarkFailed(failureCode, failureMessage, retryAt);
        if (retryAt.HasValue)
        {
            Status = IntegrationExecutionStatus.RetryScheduled;
            return;
        }

        foreach (var laterStep in Steps.Where(candidate => candidate.ExecutionOrder > executionOrder && candidate.Status == ExecutionStepStatus.Pending))
            laterStep.MarkSkipped();
        Status = IntegrationExecutionStatus.Failed;
        CompletedAt = DateTimeOffset.UtcNow;
    }

    public void Cancel()
    {
        if (Status is not (IntegrationExecutionStatus.Queued or IntegrationExecutionStatus.RetryScheduled or IntegrationExecutionStatus.Running))
            throw new InvalidOperationException("Only queued or retry-scheduled executions can be cancelled between attempts.");
        if (Steps.Any(step => step.Status == ExecutionStepStatus.Running))
            throw new InvalidOperationException("An execution cannot be cancelled while a destination attempt is in progress.");

        foreach (var pendingStep in Steps.Where(step => step.Status is ExecutionStepStatus.Pending or ExecutionStepStatus.RetryScheduled))
            pendingStep.MarkSkipped();
        Status = IntegrationExecutionStatus.Cancelled;
        CompletedAt = DateTimeOffset.UtcNow;
    }

    private void EnsureRunning()
    {
        if (Status != IntegrationExecutionStatus.Running)
            throw new InvalidOperationException("The execution must be running to complete a step.");
    }

    private ExecutionStep GetCurrentStep(int executionOrder)
    {
        var step = Steps.SingleOrDefault(candidate => candidate.ExecutionOrder == executionOrder)
            ?? throw new ArgumentOutOfRangeException(nameof(executionOrder), "The execution step does not exist.");
        var priorStepNotComplete = Steps.Any(candidate => candidate.ExecutionOrder < executionOrder && candidate.Status != ExecutionStepStatus.Succeeded);
        if (priorStepNotComplete)
            throw new InvalidOperationException("Execution steps must complete sequentially.");
        return step;
    }
}
