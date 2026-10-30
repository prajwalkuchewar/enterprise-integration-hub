using EnterpriseIntegrationHub.Domain.Common;
using EnterpriseIntegrationHub.Domain.Enums;

namespace EnterpriseIntegrationHub.Domain.Entities
{
    public class ExternalSystem : BaseEntity
    {
        public ExternalSystem(string name, string description, ExternalSystemEnvironment environment, ExternalSystemStatus status)
        {
            Name = name;
            Description = description;
            Environment = environment;
            Status = status;
        }

        public string Name { get; private set; } = string.Empty;
        public string Description { get; private set; } = string.Empty;
        public ExternalSystemEnvironment Environment { get; private set; }
        public ExternalSystemStatus Status { get; private set; }

        public void Update(string name, string description, ExternalSystemEnvironment environment)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name is required.", nameof(name));

            if (string.IsNullOrWhiteSpace(description))
                throw new ArgumentException("Description is required.", nameof(description));

            Name = name;
            Description = description;
            Environment = environment;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        public void Activate()
        {
            if (Status == ExternalSystemStatus.Active)
                throw new InvalidOperationException($"External system with ID {Id} is already active.");

            Status = ExternalSystemStatus.Active;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        public void Deactivate()
        {
            if (Status == ExternalSystemStatus.Inactive)
                throw new InvalidOperationException($"External system with ID {Id} is already inactive.");

            Status = ExternalSystemStatus.Inactive;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

    }
}
