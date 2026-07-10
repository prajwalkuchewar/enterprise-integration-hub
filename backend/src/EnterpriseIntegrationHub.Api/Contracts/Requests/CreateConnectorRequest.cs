using System.ComponentModel.DataAnnotations;
using EnterpriseIntegrationHub.Domain.Enums;

namespace EnterpriseIntegrationHub.Api.Contracts.Requests;

public sealed class CreateConnectorRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; init; } = string.Empty;

    [Required]
    [MaxLength(1000)]
    public string Description { get; init; } = string.Empty;

    [Required]
    public Guid ExternalSystemId { get; init; }

    [Required]
    [Url]
    [MaxLength(200)]
    public string BaseUrl { get; init; } = string.Empty;

    [Required]
    public ConnectorProtocol Protocol { get; init; }

    [Required]
    public ConnectorAuthenticationType AuthenticationType { get; init; }

    [Required]
    [Range(1, 3600)]
    public int TimeoutSeconds { get; init; }
    }
