using System.ComponentModel.DataAnnotations;
using EnterpriseIntegrationHub.Domain.Enums;

namespace EnterpriseIntegrationHub.Api.Contracts.Requests;

/// <summary>Request payload for updating a connector.</summary>
public sealed class UpdateConnectorRequest
{
    /// <summary>Gets the connector name.</summary>
    [MaxLength(200)]
    public string Name { get; init; } = string.Empty;

    /// <summary>Gets the connector description.</summary>
    [MaxLength(1000)]
    public string Description { get; init; } = string.Empty;

    /// <summary>Gets the connector base URL.</summary>
    [Url]
    [MaxLength(200)]
    public string BaseUrl { get; init; } = string.Empty;

    /// <summary>Gets the connector protocol.</summary>
    public ConnectorProtocol Protocol { get; init; }

    /// <summary>Gets the connector authentication type.</summary>
    public ConnectorAuthenticationType AuthenticationType { get; init; }

    /// <summary>Gets the request timeout in seconds.</summary>
    [Range(1, 3600)]
    public int TimeoutSeconds { get; init; }
}
