using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnterpriseIntegrationHub.Domain.Common
{
    public abstract class BaseEntity
    {
        public Guid Id { get; init; }

        public string? CreatedAt { get; private set; }

        public string? UpdatedAt { get; private set; }
    }
}
