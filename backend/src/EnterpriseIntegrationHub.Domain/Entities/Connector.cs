using EnterpriseIntegrationHub.Domain.Common;
using EnterpriseIntegrationHub.Domain.Enums;

namespace EnterpriseIntegrationHub.Domain.Entities
{
    public class Connector : BaseEntity
    {
        public string Name { get; private set; } = string.Empty;
        public string Description { get; private set; } = string.Empty;

        public Guid ExternalSystemId { get; private set; }

        public string BaseUrl { get; private set; } = string.Empty;
        
        public ConnectorProtocol Protocol { get; private set; }

        public ConnectorAuthenticationType AuthenticationType { get; private set; }

        public int TimeoutSeconds { get; private set; }

        public ConnectorStatus Status { get; private set; }

        public Connector(string name, string description, Guid externalSystemId, string baseUrl, ConnectorProtocol protocol, ConnectorAuthenticationType authenticationType, int timeoutSeconds, ConnectorStatus status)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Name is required.", nameof(name));
            }

            if (externalSystemId == Guid.Empty)
            {
                throw new ArgumentException("ExternalSystemId must be provided.", nameof(externalSystemId));
            }

            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                throw new ArgumentException("BaseUrl is required.", nameof(baseUrl));
            }

            if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                throw new ArgumentException("BaseUrl must be a valid absolute HTTP/HTTPS URL.", nameof(baseUrl));
            }

            Name = name;
            Description = description;
            ExternalSystemId = externalSystemId;
            BaseUrl = baseUrl;
            Protocol = protocol;
            AuthenticationType = authenticationType;
            TimeoutSeconds = timeoutSeconds;
            Status = status;
        }



    }
}
