using EnterpriseIntegrationHub.Domain.Enums;

namespace EnterpriseIntegrationHub.Application.Features.ExternalSystems.Create;

public sealed record CreateExternalSystemCommand(
    string Name,
    string Description,
    ExternalSystemEnvironment Environment);
