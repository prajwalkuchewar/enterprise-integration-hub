using EnterpriseIntegrationHub.Domain.Enums;

namespace EnterpriseIntegrationHub.Application.Contracts.Responses;

public sealed record ConnectorsResponseModel(
    IReadOnlyCollection<ConnectorSummary> Items,
    int TotalCount
);

public sealed record ConnectorSummary(
    Guid Id,
    string Name, 
    string Description,
    Guid ExternalSystemId,
    string BaseUrl,
    ConnectorProtocol Protocol,
    ConnectorAuthenticationType AuthenticationType,
    int TimeoutSeconds,
    ConnectorStatus Status
);

