
using EnterpriseIntegrationHub.Domain.Enums;

public sealed record CreateExternalSystemCommand(
    string Name,
    string Description,
    ExternalSystemEnvironment Environment
);