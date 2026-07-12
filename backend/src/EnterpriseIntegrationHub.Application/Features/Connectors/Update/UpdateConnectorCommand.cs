using EnterpriseIntegrationHub.Domain.Enums;

namespace EnterpriseIntegrationHub.Application.Features.Connectors.Update;

public sealed record UpdateConnectorCommand(
    string Name,
    string Description,
    string BaseUrl,
    ConnectorProtocol Protocol,
    ConnectorAuthenticationType AuthenticationType,
    int TimeoutSeconds);
