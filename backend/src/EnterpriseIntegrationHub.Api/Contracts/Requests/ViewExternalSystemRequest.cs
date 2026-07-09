using System.ComponentModel.DataAnnotations;

namespace EnterpriseIntegrationHub.Api.Contracts.Requests
{
    public sealed class ViewExternalSystemRequest
    {
        // You can add properties here if needed in the future
        [Required]
        public Guid Id;
    }
}
