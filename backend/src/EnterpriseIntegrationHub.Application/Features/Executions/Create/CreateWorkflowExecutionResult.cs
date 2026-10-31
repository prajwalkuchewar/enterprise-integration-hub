using EnterpriseIntegrationHub.Domain.Enums;

namespace EnterpriseIntegrationHub.Application.Features.Executions.Create;

public sealed record CreateWorkflowExecutionResult(Guid ExecutionId, IntegrationExecutionStatus Status, bool AlreadyExisted);
