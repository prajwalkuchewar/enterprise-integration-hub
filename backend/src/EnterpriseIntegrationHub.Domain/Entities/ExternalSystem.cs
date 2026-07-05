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

    }
}
