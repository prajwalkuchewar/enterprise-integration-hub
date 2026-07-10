using System;

namespace EnterpriseIntegrationHub.Application.Features.Connectors.Create;

public sealed class CreateConnectorCommandValidator
{
    public void Validate(CreateConnectorCommand command)
    {
        if (command is null)
            throw new ArgumentNullException(nameof(command));

        if (string.IsNullOrWhiteSpace(command.Name))
            throw new ArgumentException("Name is required.", nameof(command.Name));

        if (command.ExternalSystemId == Guid.Empty)
            throw new ArgumentException("ExternalSystemId must be provided.", nameof(command.ExternalSystemId));

        if (string.IsNullOrWhiteSpace(command.BaseUrl))
            throw new ArgumentException("BaseUrl is required.", nameof(command.BaseUrl));

        if (!Uri.TryCreate(command.BaseUrl, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            throw new ArgumentException("BaseUrl must be a valid absolute HTTP/HTTPS URL.", nameof(command.BaseUrl));

        if (command.TimeoutSeconds <= 0)
            throw new ArgumentException("TimeoutSeconds must be zero or positive.", nameof(command.TimeoutSeconds));
    }
}
