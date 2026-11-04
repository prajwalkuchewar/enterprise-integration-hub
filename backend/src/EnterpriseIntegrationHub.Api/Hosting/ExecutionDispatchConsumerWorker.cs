using Confluent.Kafka;
using EnterpriseIntegrationHub.Infrastructure.Messaging.Kafka;

namespace EnterpriseIntegrationHub.Api.Hosting;

/// <summary>Persists Kafka dispatches before acknowledging them.</summary>
public sealed class ExecutionDispatchConsumerWorker(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<ExecutionDispatchConsumerWorker> logger) : BackgroundService
{
    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();
        var settings = configuration.GetSection(KafkaOptions.SectionName).Get<KafkaOptions>() ?? new KafkaOptions();
        if (!settings.ConsumerEnabled)
            return;
        if (string.IsNullOrWhiteSpace(settings.BootstrapServers) ||
            string.IsNullOrWhiteSpace(settings.ExecutionDispatchTopic) ||
            string.IsNullOrWhiteSpace(settings.ExecutionDispatchConsumerGroup))
            throw new InvalidOperationException("Kafka broker, dispatch topic, and consumer group must be configured.");

        using var consumer = new ConsumerBuilder<string, string>(new ConsumerConfig
        {
            BootstrapServers = settings.BootstrapServers,
            ClientId = settings.ClientId,
            GroupId = settings.ExecutionDispatchConsumerGroup,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
            EnableAutoOffsetStore = false
        }).Build();

        consumer.Subscribe(settings.ExecutionDispatchTopic);
        logger.LogInformation("Execution dispatch consumer started for topic {Topic} and group {Group}.",
            settings.ExecutionDispatchTopic, settings.ExecutionDispatchConsumerGroup);
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var result = consumer.Consume(stoppingToken);
                await using var scope = scopeFactory.CreateAsyncScope();
                var processor = scope.ServiceProvider.GetRequiredService<ExecutionDispatchRecordProcessor>();
                try
                {
                    await processor.ProcessAsync(result, consumer, stoppingToken);
                }
                catch (InvalidDataException)
                {
                    logger.LogError("Invalid execution dispatch at {Topic} partition {Partition} offset {Offset}; consumer is stopping without committing it.",
                        result.Topic, result.Partition.Value, result.Offset.Value);
                    throw;
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // The current offset is left uncommitted if shutdown interrupts the handoff.
        }
        finally
        {
            consumer.Close();
        }
    }
}
