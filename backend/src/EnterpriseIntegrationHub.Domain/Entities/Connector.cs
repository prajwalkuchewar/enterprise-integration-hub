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
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name is required.", nameof(name));

            var changed = Name != name || Description != description;
            if (!changed)
                return false;

            Name = name;
            Description = description;
            return true;
        }

        public bool UpdateCommunication(string baseUrl, ConnectorProtocol protocol, ConnectorAuthenticationType authenticationType, int timeoutSeconds)
        {
            if (string.IsNullOrWhiteSpace(baseUrl))
                throw new ArgumentException("BaseUrl is required.", nameof(baseUrl));

            if (timeoutSeconds <= 0)
                throw new ArgumentException("TimeoutSeconds must be a positive number.", nameof(timeoutSeconds));

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
    }
}
