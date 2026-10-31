using EnterpriseIntegrationHub.Domain.Common;
using EnterpriseIntegrationHub.Domain.Enums;

namespace EnterpriseIntegrationHub.Domain.Entities
{
    public class ExternalSystem : BaseEntity
    {
        public ExternalSystem(string name, string description, ExternalSystemEnvironment environment, ExternalSystemStatus status)
        {
            Validate(name, description, environment);
            if (!Enum.IsDefined(status))
                throw new ArgumentOutOfRangeException(nameof(status), "External system status is invalid.");

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
            Validate(name, description, environment);

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

        private static void Validate(string name, string description, ExternalSystemEnvironment environment)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name is required.", nameof(name));
            if (name.Length > 200)
                throw new ArgumentException("Name cannot exceed 200 characters.", nameof(name));
            if (string.IsNullOrWhiteSpace(description))
                throw new ArgumentException("Description is required.", nameof(description));
            if (description.Length > 1000)
                throw new ArgumentException("Description cannot exceed 1000 characters.", nameof(description));
            if (!Enum.IsDefined(environment))
                throw new ArgumentOutOfRangeException(nameof(environment), "External system environment is invalid.");
        }

    }
}
