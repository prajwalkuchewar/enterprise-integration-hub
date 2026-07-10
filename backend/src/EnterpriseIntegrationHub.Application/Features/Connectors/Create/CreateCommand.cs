using EnterpriseIntegrationHub.Domain.Enums;

namespace EnterpriseIntegrationHub.Application.Features.Connectors.Create;

public sealed record CreateConnectorCommand(
    string Name,
    string Description,
    Guid ExternalSystemId,
    string BaseUrl,
    ConnectorProtocol Protocol,
    ConnectorAuthenticationType AuthenticationType,
    int TimeoutSeconds);
