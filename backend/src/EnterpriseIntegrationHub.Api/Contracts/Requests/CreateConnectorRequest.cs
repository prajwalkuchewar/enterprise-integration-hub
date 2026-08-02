using System.ComponentModel.DataAnnotations;
using EnterpriseIntegrationHub.Domain.Enums;

namespace EnterpriseIntegrationHub.Api.Contracts.Requests;

/// <summary>Request payload for creating a connector.</summary>
public sealed class CreateConnectorRequest
{
    /// <summary>Gets the connector name.</summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; init; } = string.Empty;

    /// <summary>Gets the connector description.</summary>
    [Required]
    [MaxLength(1000)]
    public string Description { get; init; } = string.Empty;

    /// <summary>Gets the identifier of the external system that owns the connector.</summary>
    [Required]
    public Guid ExternalSystemId { get; init; }

    /// <summary>Gets the connector base URL.</summary>
    [Required]
    [Url]
    [MaxLength(200)]
    public string BaseUrl { get; init; } = string.Empty;

    /// <summary>Gets the connector protocol.</summary>
    [Required]
    public ConnectorProtocol Protocol { get; init; }

    /// <summary>Gets the connector authentication type.</summary>
    [Required]
    public ConnectorAuthenticationType AuthenticationType { get; init; }

    /// <summary>Gets the request timeout in seconds.</summary>
    [Required]
    [Range(1, 3600)]
    public int TimeoutSeconds { get; init; }
}
