using System.ComponentModel.DataAnnotations;
using EnterpriseIntegrationHub.Api.Validation;
using EnterpriseIntegrationHub.Domain.Enums;

namespace EnterpriseIntegrationHub.Api.Contracts.Requests;

/// <summary>Request payload for updating an external system.</summary>
public sealed class UpdateExternalSystemRequest
{
    /// <summary>Gets the external system name.</summary>
    [NotWhiteSpace, MaxLength(200)]
    public string Name { get; init; } = string.Empty;

    /// <summary>Gets the external system description.</summary>
    [NotWhiteSpace, MaxLength(1000)]
    public string Description { get; init; } = string.Empty;

    /// <summary>Gets the target environment.</summary>
    [DefinedEnum(typeof(ExternalSystemEnvironment))]
    public ExternalSystemEnvironment Environment { get; init; }
}
