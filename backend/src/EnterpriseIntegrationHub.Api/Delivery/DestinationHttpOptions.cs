namespace EnterpriseIntegrationHub.Api.Delivery;

internal sealed class DestinationHttpOptions
{
    public const string SectionName = "DestinationHttp";
    public string[] AllowedOrigins { get; set; } = [];
    public bool AllowInsecureHttp { get; set; }
    public bool AllowLoopback { get; set; }
}
