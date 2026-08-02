namespace EnterpriseIntegrationHub.Application.Features.Connectors.Update;

public sealed class UpdateConnectorCommandValidator
{
    public void Validate(UpdateConnectorCommand command)
    {
        if (command is null)
            throw new ArgumentNullException(nameof(command));

        if (string.IsNullOrWhiteSpace(command.Name))
            throw new ArgumentException("Name is required.", nameof(command.Name));
        if (string.IsNullOrWhiteSpace(command.BaseUrl))
            throw new ArgumentException("BaseUrl is required.", nameof(command.BaseUrl));

        if (!Uri.TryCreate(command.BaseUrl, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            throw new ArgumentException("BaseUrl must be a valid absolute HTTP/HTTPS URL.", nameof(command.BaseUrl));

        if (command.TimeoutSeconds <= 0)
            throw new ArgumentException("TimeoutSeconds must be a positive number.", nameof(command.TimeoutSeconds));
    }
}
