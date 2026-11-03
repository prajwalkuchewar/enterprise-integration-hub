using System.Text;
using System.Text.Json;
using Confluent.Kafka;
using EnterpriseIntegrationHub.Application.Features.Executions.DispatchOutbox;
using EnterpriseIntegrationHub.Application.Interfaces;

namespace EnterpriseIntegrationHub.Infrastructure.Messaging.Kafka;

public sealed class KafkaExecutionDispatchPublisher : IExecutionDispatchPublisher, IDisposable
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly IProducer<string, string> _producer;
    private readonly string _topic;

    public KafkaExecutionDispatchPublisher(KafkaOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _topic = options.ExecutionDispatchTopic;
        _producer = new ProducerBuilder<string, string>(new ProducerConfig
        {
            BootstrapServers = options.BootstrapServers,
            ClientId = options.ClientId,
            Acks = Acks.All,
            EnableIdempotence = true,
            MessageTimeoutMs = options.MessageTimeoutMs,
        }).Build();
    }

    public async Task PublishAsync(ExecutionDispatchMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);

        var kafkaMessage = new Message<string, string>
        {
            // Keying by execution keeps all dispatches for one execution on the same partition, in order.
            Key = message.ExecutionId.ToString("D"),
            Value = JsonSerializer.Serialize(message, SerializerOptions),
            Headers = new Headers
            {
                { "dispatch-id", Encoding.UTF8.GetBytes(message.DispatchId.ToString("D")) },
                { "correlation-id", Encoding.UTF8.GetBytes(message.CorrelationId ?? string.Empty) },
                { "event-type", Encoding.UTF8.GetBytes("execution.dispatch") },
            },
        };

        await _producer.ProduceAsync(_topic, kafkaMessage, cancellationToken);
    }

    public void Dispose()
    {
        _producer.Flush(TimeSpan.FromSeconds(5));
        _producer.Dispose();
    }
}
