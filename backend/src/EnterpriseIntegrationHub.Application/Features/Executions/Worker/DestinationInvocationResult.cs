namespace EnterpriseIntegrationHub.Application.Features.Executions.Worker;

public enum DestinationInvocationStatus
{
    Succeeded,
    TransientFailure,
    PermanentFailure,
    InvalidConfiguration
}

public sealed record DestinationInvocationResult(
    DestinationInvocationStatus Status,
    int? HttpStatusCode = null,
    long? DurationMilliseconds = null)
{
    public static DestinationInvocationResult Succeeded => new(DestinationInvocationStatus.Succeeded);
    public static DestinationInvocationResult TransientFailure => new(DestinationInvocationStatus.TransientFailure);
    public static DestinationInvocationResult PermanentFailure => new(DestinationInvocationStatus.PermanentFailure);
    public static DestinationInvocationResult InvalidConfiguration => new(DestinationInvocationStatus.InvalidConfiguration);
}
