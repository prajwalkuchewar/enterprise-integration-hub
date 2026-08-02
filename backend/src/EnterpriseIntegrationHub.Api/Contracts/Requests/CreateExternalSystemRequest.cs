using System.ComponentModel.DataAnnotations;
using EnterpriseIntegrationHub.Domain.Enums;

namespace EnterpriseIntegrationHub.Api.Contracts.Requests;

/// <summary>Request payload for creating an external system.</summary>
public sealed class CreateExternalSystemRequest
{
    /// <summary>Gets the external system name.</summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; init; } = string.Empty;

    /// <summary>Gets the external system description.</summary>
    [Required]
    [MaxLength(1000)]
    public string Description { get; init; } = string.Empty;

    /// <summary>Gets the target environment.</summary>
    [Required]
    public ExternalSystemEnvironment Environment { get; init; }
}
