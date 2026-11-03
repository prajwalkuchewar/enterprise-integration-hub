using Confluent.Kafka;

namespace EnterpriseIntegrationHub.Infrastructure.Messaging.Kafka;

public sealed class ExecutionDispatchRecordProcessor(IExecutionDispatchInboxWriter writer)
{
    public async Task ProcessAsync(
        ConsumeResult<string, string> result,
        IConsumer<string, string> consumer,
        CancellationToken cancellationToken)
    {
        var dispatch = ExecutionDispatchMessageReader.Read(result.Message);
        await writer.StoreAsync(dispatch, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        consumer.Commit(result);
    }
}
