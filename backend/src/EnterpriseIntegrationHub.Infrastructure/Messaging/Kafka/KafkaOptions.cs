namespace EnterpriseIntegrationHub.Infrastructure.Messaging.Kafka;

public sealed class KafkaOptions
{
    public const string SectionName = "Kafka";

    public string BootstrapServers { get; set; } = "localhost:9092";
    public string ClientId { get; set; } = "eih-api";
    public string ExecutionDispatchTopic { get; set; } = "eih.execution.dispatch.v1";
    public int MessageTimeoutMs { get; set; } = 30000;
}
