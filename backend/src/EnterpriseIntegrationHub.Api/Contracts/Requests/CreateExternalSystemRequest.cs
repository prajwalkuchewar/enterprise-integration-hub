using System.ComponentModel.DataAnnotations;
using EnterpriseIntegrationHub.Domain.Enums;

namespace EnterpriseIntegrationHub.Api.Contracts.Requests;

public sealed class CreateExternalSystemRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; init; } = string.Empty;

    [Required]
    [MaxLength(1000)]
    public string Description { get; init; } = string.Empty;

    [Required]
    public ExternalSystemEnvironment Environment { get; init; }
}
