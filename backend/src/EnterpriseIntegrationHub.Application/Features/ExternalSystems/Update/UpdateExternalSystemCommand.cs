using EnterpriseIntegrationHub.Domain.Enums;

namespace EnterpriseIntegrationHub.Application.Features.ExternalSystems.Update;

public sealed record UpdateExternalSystemCommand(string Name, string Description, ExternalSystemEnvironment Environment);
