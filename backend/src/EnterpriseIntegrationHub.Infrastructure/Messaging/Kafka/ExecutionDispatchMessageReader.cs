using System.Text;
using System.Text.Json;
using Confluent.Kafka;
using EnterpriseIntegrationHub.Application.Features.Executions.DispatchOutbox;

namespace EnterpriseIntegrationHub.Infrastructure.Messaging.Kafka;

public static class ExecutionDispatchMessageReader
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private static readonly Encoding StrictUtf8 = new UTF8Encoding(false, true);

    public static ExecutionDispatchMessage Read(Message<string, string> kafkaMessage)
    {
        ArgumentNullException.ThrowIfNull(kafkaMessage);
        if (kafkaMessage.Value is null)
            throw new InvalidDataException("Dispatch payload is missing.");
        ExecutionDispatchMessage? dispatch;
        try
        {
            dispatch = JsonSerializer.Deserialize<ExecutionDispatchMessage>(kafkaMessage.Value, SerializerOptions);
        }
        catch (JsonException)
        {
            throw new InvalidDataException("Dispatch payload is not valid JSON.");
        }

        if (dispatch is null || dispatch.DispatchId == Guid.Empty || dispatch.ExecutionId == Guid.Empty ||
            dispatch.WorkflowId == Guid.Empty || dispatch.SourceConnectorId == Guid.Empty ||
            dispatch.Attempt < 1 || dispatch.WorkflowRevisionNumber < 1 ||
            string.IsNullOrWhiteSpace(dispatch.SourceEventId) || string.IsNullOrWhiteSpace(dispatch.TriggerEvent) ||
            dispatch.OccurredAt == default ||
            !Guid.TryParse(kafkaMessage.Key, out var key) || key != dispatch.ExecutionId)
            throw new InvalidDataException("Dispatch payload or message key is invalid.");

        if (ReadHeader(kafkaMessage.Headers, "event-type") != "execution.dispatch" ||
            !Guid.TryParse(ReadHeader(kafkaMessage.Headers, "dispatch-id"), out var headerId) ||
            headerId != dispatch.DispatchId ||
            ReadHeader(kafkaMessage.Headers, "correlation-id") != (dispatch.CorrelationId ?? string.Empty))
            throw new InvalidDataException("Dispatch headers do not match the payload.");

        return dispatch;
    }

    private static string ReadHeader(Headers? headers, string key)
    {
        var matches = headers?.Where(header => header.Key == key).ToArray();
        if (matches is null || matches.Length != 1 || matches[0].GetValueBytes() is not { } bytes)
            throw new InvalidDataException("Dispatch has missing or repeated required headers.");
        try
        {
            return StrictUtf8.GetString(bytes);
        }
        catch (DecoderFallbackException exception)
        {
            throw new InvalidDataException("Dispatch header is not valid UTF-8.", exception);
        }
    }
}
