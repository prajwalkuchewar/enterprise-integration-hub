using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using EnterpriseIntegrationHub.Api.Hosting;
using EnterpriseIntegrationHub.Application.Features.Executions.DispatchOutbox;
using EnterpriseIntegrationHub.Domain.Entities;
using EnterpriseIntegrationHub.Domain.Enums;
using EnterpriseIntegrationHub.Infrastructure.Messaging.Kafka;
using EnterpriseIntegrationHub.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace EnterpriseIntegrationHub.Api.Tests;

public sealed class ExecutionDispatchConsumerApiTests
{
    [KafkaIntegrationFact]
    public async Task HostedConsumer_StoresPublishedDispatchBeforeCommittingOffset()
    {
        var bootstrap = Environment.GetEnvironmentVariable("EIH_KAFKA_TEST_BOOTSTRAP")!;
        var topic = $"eih-test-host-{Guid.NewGuid():N}";
        var group = $"eih-test-host-group-{Guid.NewGuid():N}";
        using var admin = new AdminClientBuilder(new AdminClientConfig { BootstrapServers = bootstrap }).Build();
        await admin.CreateTopicsAsync([new TopicSpecification { Name = topic, NumPartitions = 1, ReplicationFactor = 1 }]);

        try
        {
            using var baseFactory = new EnterpriseIntegrationHubApiFactory();
            using var factory = baseFactory.WithWebHostBuilder(builder =>
                builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["Kafka:ConsumerEnabled"] = "true",
                        ["Kafka:BootstrapServers"] = bootstrap,
                        ["Kafka:ExecutionDispatchTopic"] = topic,
                        ["Kafka:ExecutionDispatchConsumerGroup"] = group
                    })));
            using var client = factory.CreateClient();
            factory.Services.GetRequiredService<IConfiguration>().GetValue<bool>("Kafka:ConsumerEnabled").Should().BeTrue();
            factory.Services.GetServices<IHostedService>().OfType<ExecutionDispatchConsumerWorker>().Should().ContainSingle();
            ExecutionDispatchMessage message;
            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<EnterpriseIntegrationHubDbContext>();
                var system = new ExternalSystem("Orders", "Test system", ExternalSystemEnvironment.Testing, ExternalSystemStatus.Active);
                var source = new Connector("Source", "Test source", system.Id, "https://source.example.test",
                    ConnectorProtocol.REST, ConnectorAuthenticationType.APIKey, 30, ConnectorStatus.Active);
                var destination = new Connector("Destination", "Test destination", system.Id, "https://destination.example.test",
                    ConnectorProtocol.REST, ConnectorAuthenticationType.APIKey, 30, ConnectorStatus.Active);
                var workflow = new Workflow("Test workflow", source.Id, "order.created", [new(destination.Id, 1)]);
                workflow.Activate();
                var revision = new WorkflowRevision(workflow);
                var revisionStep = revision.Steps.Single();
                var execution = new IntegrationExecution(revision, source.Id, "event-1", DateTimeOffset.UtcNow, "correlation-1",
                    new string('a', 64), [new ExecutionStepSnapshot(revisionStep.Id, destination.Id, destination.BaseUrl,
                        destination.Protocol, destination.AuthenticationType, destination.TimeoutSeconds, revisionStep.ExecutionOrder)]);
                using var payload = JsonDocument.Parse("{\"value\":42}");
                var receipt = new EventReceipt(execution, 1, payload.RootElement);
                var outbox = new ExecutionDispatchOutbox(execution.Id);
                context.ExternalSystems.Add(system);
                context.Connectors.AddRange(source, destination);
                context.Workflows.Add(workflow);
                context.WorkflowRevisions.Add(revision);
                context.EventReceipts.Add(receipt);
                context.IntegrationExecutions.Add(execution);
                context.ExecutionDispatchOutbox.Add(outbox);
                await context.SaveChangesAsync();
                message = new ExecutionDispatchMessage(outbox.Id, 1, execution.Id, execution.WorkflowId,
                    execution.WorkflowRevisionNumber, execution.SourceConnectorId, execution.SourceEventId,
                    execution.TriggerEvent, execution.OccurredAt, execution.CorrelationId);
            }

            using (var publisher = new KafkaExecutionDispatchPublisher(new KafkaOptions
            {
                BootstrapServers = bootstrap,
                ExecutionDispatchTopic = topic
            }))
                await publisher.PublishAsync(message, default);

            var deadline = DateTimeOffset.UtcNow.AddSeconds(60);
            var stored = false;
            while (DateTimeOffset.UtcNow < deadline && !stored)
            {
                using var scope = factory.Services.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<EnterpriseIntegrationHubDbContext>();
                stored = await context.ExecutionDispatchInbox.AnyAsync(item => item.DispatchId == message.DispatchId);
                if (!stored)
                    await Task.Delay(100);
            }
            stored.Should().BeTrue("the hosted consumer must hand off the Kafka dispatch");

            using var observer = new ConsumerBuilder<string, string>(new ConsumerConfig
            {
                BootstrapServers = bootstrap,
                GroupId = group,
                EnableAutoCommit = false
            }).Build();
            var committed = false;
            while (DateTimeOffset.UtcNow < deadline && !committed)
            {
                committed = observer.Committed([new TopicPartition(topic, 0)], TimeSpan.FromSeconds(5))
                    .Single().Offset.Value == 1;
                if (!committed)
                    await Task.Delay(100);
            }
            committed.Should().BeTrue("the offset must be committed after handoff");
        }
        finally
        {
            await admin.DeleteTopicsAsync([topic]);
        }
    }

    private sealed class KafkaIntegrationFactAttribute : FactAttribute
    {
        public KafkaIntegrationFactAttribute()
        {
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("EIH_KAFKA_TEST_BOOTSTRAP")))
                Skip = "Set EIH_KAFKA_TEST_BOOTSTRAP to run the Kafka integration test.";
        }
    }
}
