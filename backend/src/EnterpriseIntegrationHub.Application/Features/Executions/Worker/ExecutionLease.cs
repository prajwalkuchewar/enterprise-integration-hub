namespace EnterpriseIntegrationHub.Application.Features.Executions.Worker;

public sealed record ExecutionLease(
    Guid DispatchId,
    Guid ExecutionId,
    Guid Token,
    DateTimeOffset ExpiresAt);
