using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using EnterpriseIntegrationHub.Application.Features.Executions.DispatchOutbox;
using EnterpriseIntegrationHub.Infrastructure.Messaging.Kafka;
using FluentAssertions;
using Moq;
using Xunit;

namespace EnterpriseIntegrationHub.Infrastructure.Tests;

public sealed class ExecutionDispatchRecordProcessorTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task Process_StoresBeforeCommitting()
    {
        var message = CreateDispatch();
        var record = CreateRecord(message);
        var stored = false;
        var writer = new Mock<IExecutionDispatchInboxWriter>();
        writer.Setup(item => item.StoreAsync(message, It.IsAny<CancellationToken>()))
            .Callback(() => stored = true)
            .Returns(Task.CompletedTask);
        var consumer = new Mock<IConsumer<string, string>>();
        consumer.Setup(item => item.Commit(record)).Callback(() => stored.Should().BeTrue());

        await new ExecutionDispatchRecordProcessor(writer.Object).ProcessAsync(record, consumer.Object, default);

        writer.Verify(item => item.StoreAsync(message, It.IsAny<CancellationToken>()), Times.Once);
        consumer.Verify(item => item.Commit(record), Times.Once);
    }

    [Fact]
    public async Task Process_WhenPersistenceFails_DoesNotCommit()
    {
        var record = CreateRecord(CreateDispatch());
        var writer = new Mock<IExecutionDispatchInboxWriter>();
        writer.Setup(item => item.StoreAsync(It.IsAny<ExecutionDispatchMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("Storage unavailable."));
        var consumer = new Mock<IConsumer<string, string>>();

        var act = () => new ExecutionDispatchRecordProcessor(writer.Object).ProcessAsync(record, consumer.Object, default);

        await act.Should().ThrowAsync<IOException>();
        consumer.Verify(item => item.Commit(It.IsAny<ConsumeResult<string, string>>()), Times.Never);
    }

    [Fact]
    public async Task Process_WhenCommitFails_PropagatesFailureAfterStorage()
    {
        var record = CreateRecord(CreateDispatch());
        var writer = new Mock<IExecutionDispatchInboxWriter>();
        writer.Setup(item => item.StoreAsync(It.IsAny<ExecutionDispatchMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var consumer = new Mock<IConsumer<string, string>>();
        consumer.Setup(item => item.Commit(record)).Throws(new InvalidOperationException("Commit failed."));

        var act = () => new ExecutionDispatchRecordProcessor(writer.Object).ProcessAsync(record, consumer.Object, default);

        await act.Should().ThrowAsync<InvalidOperationException>();
        writer.Verify(item => item.StoreAsync(It.IsAny<ExecutionDispatchMessage>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Process_WhenCancelledAfterPersistence_DoesNotCommit()
    {
        using var cancellation = new CancellationTokenSource();
        var record = CreateRecord(CreateDispatch());
        var writer = new Mock<IExecutionDispatchInboxWriter>();
        writer.Setup(item => item.StoreAsync(It.IsAny<ExecutionDispatchMessage>(), It.IsAny<CancellationToken>()))
            .Callback(() => cancellation.Cancel()).Returns(Task.CompletedTask);
        var consumer = new Mock<IConsumer<string, string>>();

        var act = () => new ExecutionDispatchRecordProcessor(writer.Object).ProcessAsync(record, consumer.Object, cancellation.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        consumer.Verify(item => item.Commit(It.IsAny<ConsumeResult<string, string>>()), Times.Never);
    }

    [Theory]
    [InlineData("invalid-json")]
    [InlineData("null-value")]
    [InlineData("wrong-key")]
    [InlineData("wrong-header")]
    [InlineData("missing-header")]
    [InlineData("wrong-correlation")]
    public async Task Process_WhenMessageIsInvalid_DoesNotStoreOrCommit(string failure)
    {
        var record = CreateRecord(CreateDispatch());
        switch (failure)
        {
            case "invalid-json": record.Message.Value = "{not-json"; break;
            case "null-value": record.Message.Value = null!; break;
            case "wrong-key": record.Message.Key = Guid.NewGuid().ToString(); break;
            case "wrong-header": record.Message.Headers.Add("dispatch-id", Encoding.UTF8.GetBytes(Guid.NewGuid().ToString())); break;
            case "missing-header": record.Message.Headers = new Headers(); break;
            case "wrong-correlation":
                record.Message.Headers.Remove("correlation-id");
                record.Message.Headers.Add("correlation-id", Encoding.UTF8.GetBytes("wrong"));
                break;
        }
        var writer = new Mock<IExecutionDispatchInboxWriter>();
        var consumer = new Mock<IConsumer<string, string>>();

        var act = () => new ExecutionDispatchRecordProcessor(writer.Object).ProcessAsync(record, consumer.Object, default);

        await act.Should().ThrowAsync<InvalidDataException>();
        writer.Verify(item => item.StoreAsync(It.IsAny<ExecutionDispatchMessage>(), It.IsAny<CancellationToken>()), Times.Never);
        consumer.Verify(item => item.Commit(It.IsAny<ConsumeResult<string, string>>()), Times.Never);
    }

    private static ExecutionDispatchMessage CreateDispatch() =>
        new(Guid.NewGuid(), 1, Guid.NewGuid(), Guid.NewGuid(), 1, Guid.NewGuid(),
            "event-1", "order.created", DateTimeOffset.UtcNow, "correlation-1");

    private static ConsumeResult<string, string> CreateRecord(ExecutionDispatchMessage dispatch) => new()
    {
        Message = new Message<string, string>
        {
            Key = dispatch.ExecutionId.ToString("D"),
            Value = JsonSerializer.Serialize(dispatch, JsonOptions),
            Headers = new Headers
            {
                { "dispatch-id", Encoding.UTF8.GetBytes(dispatch.DispatchId.ToString("D")) },
                { "correlation-id", Encoding.UTF8.GetBytes(dispatch.CorrelationId ?? string.Empty) },
                { "event-type", Encoding.UTF8.GetBytes("execution.dispatch") }
            }
        }
    };
}
