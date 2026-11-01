using EnterpriseIntegrationHub.Domain.Enums;

namespace EnterpriseIntegrationHub.Domain.Entities;

public sealed class ExecutionStep
{
    private ExecutionStep() { }

    public ExecutionStep(Guid integrationExecutionId, ExecutionStepSnapshot snapshot)
    {
        if (integrationExecutionId == Guid.Empty)
            throw new ArgumentException("IntegrationExecutionId must be provided.", nameof(integrationExecutionId));
        ArgumentNullException.ThrowIfNull(snapshot);

        Id = Guid.NewGuid();
        IntegrationExecutionId = integrationExecutionId;
        WorkflowRevisionStepId = snapshot.WorkflowRevisionStepId;
        DestinationConnectorId = snapshot.DestinationConnectorId;
        DestinationBaseUrl = snapshot.BaseUrl;
        Protocol = snapshot.Protocol;
        AuthenticationType = snapshot.AuthenticationType;
        TimeoutSeconds = snapshot.TimeoutSeconds;
        ExecutionOrder = snapshot.ExecutionOrder;
        Status = ExecutionStepStatus.Pending;
    }

    public Guid Id { get; private set; }
    public Guid IntegrationExecutionId { get; private set; }
    public Guid WorkflowRevisionStepId { get; private set; }
    public Guid DestinationConnectorId { get; private set; }
    public string DestinationBaseUrl { get; private set; } = string.Empty;
    public ConnectorProtocol Protocol { get; private set; }
    public ConnectorAuthenticationType AuthenticationType { get; private set; }
    public int TimeoutSeconds { get; private set; }
    public int ExecutionOrder { get; private set; }
    public ExecutionStepStatus Status { get; private set; }
    public DateTimeOffset? NextAttemptAt { get; private set; }
    public ICollection<ExecutionStepAttempt> Attempts { get; private set; } = new List<ExecutionStepAttempt>();

    public ExecutionStepAttempt StartAttempt()
    {
        if (Status is not (ExecutionStepStatus.Pending or ExecutionStepStatus.RetryScheduled))
            throw new InvalidOperationException("Only a pending or retry-scheduled step can start an attempt.");

        var attempt = new ExecutionStepAttempt(Id, Attempts.Count + 1);
        Attempts.Add(attempt);
        Status = ExecutionStepStatus.Running;
        NextAttemptAt = null;
        return attempt;
    }

    public void MarkSucceeded()
    {
        var attempt = GetRunningAttempt();
        attempt.Succeed();
        Status = ExecutionStepStatus.Succeeded;
    }

    public void MarkFailed(string failureCode, string failureMessage, DateTimeOffset? retryAt)
    {
        var attempt = GetRunningAttempt();
        attempt.Fail(failureCode, failureMessage);
        Status = retryAt.HasValue ? ExecutionStepStatus.RetryScheduled : ExecutionStepStatus.Failed;
        NextAttemptAt = retryAt;
    }

    public void MarkSkipped()
    {
        if (Status is not (ExecutionStepStatus.Pending or ExecutionStepStatus.RetryScheduled))
            throw new InvalidOperationException("Only a pending or retry-scheduled step can be skipped.");
        Status = ExecutionStepStatus.Skipped;
        NextAttemptAt = null;
    }

    private ExecutionStepAttempt GetRunningAttempt() => Attempts.LastOrDefault(attempt => attempt.Status == ExecutionStepAttemptStatus.Running)
        ?? throw new InvalidOperationException("The step has no running attempt to complete.");
}
