using EnterpriseIntegrationHub.Domain.Enums;

namespace EnterpriseIntegrationHub.Application.Contracts.Responses;

public sealed record BrowseExternalSystemsResponseModel(
    IReadOnlyCollection<ExternalSystemSummary> Items,
    int TotalCount
);

public sealed record ExternalSystemSummary(
    Guid Id,
    string Name,
    string Description,
    ExternalSystemEnvironment Environment,
    ExternalSystemStatus Status
);

