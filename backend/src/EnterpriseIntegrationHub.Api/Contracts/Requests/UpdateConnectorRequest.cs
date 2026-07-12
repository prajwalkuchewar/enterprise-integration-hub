using System.ComponentModel.DataAnnotations;
using EnterpriseIntegrationHub.Domain.Enums;

namespace EnterpriseIntegrationHub.Api.Contracts.Requests;

public sealed class UpdateConnectorRequest
{
    [MaxLength(200)]
    public string Name { get; init; } = string.Empty;

    [MaxLength(1000)]
    public string Description { get; init; } = string.Empty;

    [Url]
    [MaxLength(200)]
    public string BaseUrl { get; init; } = string.Empty;

    public ConnectorProtocol Protocol { get; init; }

    public ConnectorAuthenticationType AuthenticationType { get; init; }

    [Range(1, 3600)]
    public int TimeoutSeconds { get; init; }
}
