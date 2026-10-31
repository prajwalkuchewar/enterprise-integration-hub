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
            ValidateBasicInfo(name, description);
            ValidateCommunication(baseUrl, protocol, authenticationType, timeoutSeconds);

            if (externalSystemId == Guid.Empty)
                throw new ArgumentException("ExternalSystemId must be provided.", nameof(externalSystemId));
            if (!Enum.IsDefined(status))
                throw new ArgumentOutOfRangeException(nameof(status), "Connector status is invalid.");

            Name = name;
            Description = description;
            ExternalSystemId = externalSystemId;
            BaseUrl = baseUrl;
            Protocol = protocol;
            AuthenticationType = authenticationType;
            TimeoutSeconds = timeoutSeconds;
            Status = status;
        }

        public bool UpdateBasicInfo(string name, string description)
        {
            ValidateBasicInfo(name, description);

            var changed = Name != name || Description != description;
            if (!changed)
                return false;

            Name = name;
            Description = description;
            return true;
        }

        public bool UpdateCommunication(string baseUrl, ConnectorProtocol protocol, ConnectorAuthenticationType authenticationType, int timeoutSeconds)
        {
            ValidateCommunication(baseUrl, protocol, authenticationType, timeoutSeconds);

            var commChanged = BaseUrl != baseUrl || Protocol != protocol || AuthenticationType != authenticationType || TimeoutSeconds != timeoutSeconds;
            if (!commChanged)
                return false;

            BaseUrl = baseUrl;
            Protocol = protocol;
            AuthenticationType = authenticationType;
            TimeoutSeconds = timeoutSeconds;

            if (Status == ConnectorStatus.Active)
            {
                Status = ConnectorStatus.Draft;
            }

            return true;
        }

        public void MarkUpdated()
        {
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        public void Activate()
        {
            if (Status != ConnectorStatus.Draft)
            {
                throw new InvalidOperationException($"Connector with ID {Id} must be in Draft status to activate.");
            }

            Status = ConnectorStatus.Active;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        public void Deactivate()
        {
            if (Status != ConnectorStatus.Active)
                throw new InvalidOperationException($"Connector with ID {Id} must be active to deactivate.");

            Status = ConnectorStatus.Inactive;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        private static void ValidateBasicInfo(string name, string description)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name is required.", nameof(name));
            if (name.Length > 200)
                throw new ArgumentException("Name cannot exceed 200 characters.", nameof(name));
            if (string.IsNullOrWhiteSpace(description))
                throw new ArgumentException("Description is required.", nameof(description));
            if (description.Length > 1000)
                throw new ArgumentException("Description cannot exceed 1000 characters.", nameof(description));
        }

        private static void ValidateCommunication(string baseUrl, ConnectorProtocol protocol, ConnectorAuthenticationType authenticationType, int timeoutSeconds)
        {
            if (string.IsNullOrWhiteSpace(baseUrl))
                throw new ArgumentException("BaseUrl is required.", nameof(baseUrl));
            if (baseUrl.Length > 1000)
                throw new ArgumentException("BaseUrl cannot exceed 1000 characters.", nameof(baseUrl));
            if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
                throw new ArgumentException("BaseUrl must be a valid absolute HTTP/HTTPS URL.", nameof(baseUrl));
            if (!Enum.IsDefined(protocol))
                throw new ArgumentOutOfRangeException(nameof(protocol), "Connector protocol is invalid.");
            if (!Enum.IsDefined(authenticationType))
                throw new ArgumentOutOfRangeException(nameof(authenticationType), "Connector authentication type is invalid.");
            if (timeoutSeconds <= 0)
                throw new ArgumentException("TimeoutSeconds must be a positive number.", nameof(timeoutSeconds));
        }
    }
}
