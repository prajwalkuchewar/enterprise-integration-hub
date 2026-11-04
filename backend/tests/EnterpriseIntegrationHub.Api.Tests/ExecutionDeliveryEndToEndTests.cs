using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Text.Json;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using EnterpriseIntegrationHub.Api.Hosting;
using EnterpriseIntegrationHub.Application.Security;
using EnterpriseIntegrationHub.Domain.Entities;
using EnterpriseIntegrationHub.Domain.Enums;
using EnterpriseIntegrationHub.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace EnterpriseIntegrationHub.Api.Tests;

public sealed class ExecutionDeliveryEndToEndTests
{
    [Fact]
    public async Task HostedWorker_IsDisabledByDefault()
    {
        using var factory = new EnterpriseIntegrationHubApiFactory();
        using var client = factory.CreateClient();
        var worker = factory.Services.GetServices<IHostedService>().OfType<ExecutionWorker>().Single();
        await worker.ExecuteTask!.WaitAsync(TimeSpan.FromSeconds(5));
        worker.ExecuteTask.IsCompletedSuccessfully.Should().BeTrue();
    }

    [DeliveryIntegrationFact]
    public async Task Ingestion_ProducesOrderedHttpDeliveryWithRetryAndStableIdempotency()
    {
        var sql = Environment.GetEnvironmentVariable("EIH_SQLSERVER_TEST_CONNECTION")!;
        var kafka = Environment.GetEnvironmentVariable("EIH_KAFKA_TEST_BOOTSTRAP")!;
        var databaseName = $"EihE2e_{Guid.NewGuid():N}";
        var dbConnection = new SqlConnectionStringBuilder(sql) { InitialCatalog = databaseName }.ConnectionString;
        var masterConnection = new SqlConnectionStringBuilder(sql) { InitialCatalog = "master" }.ConnectionString;
        var topic = $"eih-e2e-{Guid.NewGuid():N}";
        var group = $"eih-e2e-group-{Guid.NewGuid():N}";
        var dbOptions = new DbContextOptionsBuilder<EnterpriseIntegrationHubDbContext>().UseSqlServer(dbConnection).Options;
        using var admin = new AdminClientBuilder(new AdminClientConfig { BootstrapServers = kafka }).Build();
        await admin.CreateTopicsAsync([new TopicSpecification { Name = topic, NumPartitions = 1, ReplicationFactor = 1 }]);

        try
        {
            await using (var migration = new EnterpriseIntegrationHubDbContext(dbOptions))
                await migration.Database.MigrateAsync();

            var builder = WebApplication.CreateSlimBuilder();
            builder.WebHost.ConfigureKestrel(server => server.Listen(IPAddress.Loopback, 0));
            await using var mock = builder.Build();
            var calls = new ConcurrentQueue<(string Path, string Method, string? Key, string? Credential, string Body)>();
            var firstAttempts = 0;
            var interruptNext = 1;
            var interruptedRequest = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            mock.MapMethods("/{step}", ["POST", "PUT", "PATCH"], async context =>
            {
                using var reader = new StreamReader(context.Request.Body);
                var body = await reader.ReadToEndAsync();
                calls.Enqueue((context.Request.Path.Value!, context.Request.Method, context.Request.Headers["Idempotency-Key"].FirstOrDefault(),
                    context.Request.Headers["X-API-Key"].FirstOrDefault(), body));
                if (body.Contains("\"amount\":99") && context.Request.Path == "/first" &&
                    Interlocked.Exchange(ref interruptNext, 0) == 1)
                {
                    interruptedRequest.TrySetResult();
                    await Task.Delay(Timeout.InfiniteTimeSpan, context.RequestAborted);
                }
                if (context.Request.Path == "/first" && Interlocked.Increment(ref firstAttempts) == 1)
                    context.Response.StatusCode = 503;
            });
            await mock.StartAsync();
            var origin = mock.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
            const string ingestionKey = "eih_e2e_test_key";
            var system = new ExternalSystem("Orders", "Test system", ExternalSystemEnvironment.Testing, ExternalSystemStatus.Active);
            var source = new Connector("Source", "Test source", system.Id, "https://source.example.test",
                ConnectorProtocol.REST, ConnectorAuthenticationType.APIKey, 5, ConnectorStatus.Active);
            source.AssignIngestionKeyHash(IngestionKeys.Hash(ingestionKey));
            var first = new Connector("First", "Test destination", system.Id, $"{origin}/first",
                ConnectorProtocol.REST, ConnectorAuthenticationType.APIKey, 5, ConnectorStatus.Active, ConnectorHttpMethod.Put);
            var second = new Connector("Second", "Test destination", system.Id, $"{origin}/second",
                ConnectorProtocol.REST, ConnectorAuthenticationType.APIKey, 5, ConnectorStatus.Active, ConnectorHttpMethod.Patch);
            var workflow = new Workflow("Test workflow", source.Id, "order.created", [new(first.Id, 1), new(second.Id, 2)]);
            workflow.Activate();
            await using (var seed = new EnterpriseIntegrationHubDbContext(dbOptions))
            {
                seed.ExternalSystems.Add(system);
                seed.Connectors.AddRange(source, first, second);
                seed.Workflows.Add(workflow);
                seed.WorkflowRevisions.Add(new WorkflowRevision(workflow));
                await seed.SaveChangesAsync();
            }

            var settings = new Dictionary<string, string?>
            {
                ["Outbox:Enabled"] = "true",
                ["Outbox:PollIntervalSeconds"] = "1",
                ["Kafka:ConsumerEnabled"] = "true",
                ["Kafka:BootstrapServers"] = kafka,
                ["Kafka:ExecutionDispatchTopic"] = topic,
                ["Kafka:ExecutionDispatchConsumerGroup"] = group,
                ["ExecutionWorker:Enabled"] = "true",
                ["ExecutionWorker:PollIntervalSeconds"] = "1",
                ["ExecutionWorker:BaseRetryDelaySeconds"] = "1",
                ["ExecutionWorker:MaxRetryDelaySeconds"] = "1",
                ["DestinationHttp:AllowedOrigins:0"] = origin,
                ["DestinationHttp:AllowInsecureHttp"] = "true",
                ["DestinationHttp:AllowLoopback"] = "true",
                [$"DestinationCredentials:{first.Id:D}:HeaderName"] = "X-API-Key",
                [$"DestinationCredentials:{first.Id:D}:ApiKey"] = "first-test-key",
                [$"DestinationCredentials:{second.Id:D}:HeaderName"] = "X-API-Key",
                [$"DestinationCredentials:{second.Id:D}:ApiKey"] = "second-test-key"
            };
            using var baseFactory = new EnterpriseIntegrationHubApiFactory();
            WebApplicationFactory<Program> CreateFactory() => baseFactory.WithWebHostBuilder(host =>
            {
                host.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(settings));
                host.ConfigureTestServices(services =>
                {
                    services.RemoveAll<EnterpriseIntegrationHubDbContext>();
                    services.RemoveAll<DbContextOptions<EnterpriseIntegrationHubDbContext>>();
                    services.RemoveAll<IDbContextOptionsConfiguration<EnterpriseIntegrationHubDbContext>>();
                    services.AddDbContext<EnterpriseIntegrationHubDbContext>(options => options.UseSqlServer(dbConnection));
                });
            });
            Guid recoveryExecutionId;
            using (var factory = CreateFactory())
            using (var client = factory.CreateClient())
            {
                async Task<HttpResponseMessage> SubmitAsync(string eventId, int amount)
                {
                    var json = JsonSerializer.Serialize(new
                    {
                        eventId,
                        eventType = "order.created",
                        sourceSystem = "Orders",
                        occurredAt = "2026-01-15T10:30:00Z",
                        correlationId = "e2e-correlation",
                        schemaVersion = 1,
                        payload = new { amount }
                    });
                    using var request = new HttpRequestMessage(HttpMethod.Post, "/api/events")
                    {
                        Content = new StringContent(json, Encoding.UTF8, "application/json")
                    };
                    request.Headers.Add("X-Ingestion-Key", ingestionKey);
                    return await client.SendAsync(request);
                }
                using var accepted = await SubmitAsync("e2e-event", 42);
                accepted.StatusCode.Should().Be(HttpStatusCode.Accepted);
                using var duplicate = await SubmitAsync("e2e-event", 42);
                duplicate.StatusCode.Should().Be(HttpStatusCode.OK);

                var deadline = DateTimeOffset.UtcNow.AddSeconds(90);
                IntegrationExecutionStatus? status = null;
                while (DateTimeOffset.UtcNow < deadline && status != IntegrationExecutionStatus.Succeeded)
                {
                    await using var poll = new EnterpriseIntegrationHubDbContext(dbOptions);
                    status = await poll.IntegrationExecutions.Select(execution => (IntegrationExecutionStatus?)execution.Status).SingleOrDefaultAsync();
                    if (status != IntegrationExecutionStatus.Succeeded)
                        await Task.Delay(250);
                }
                status.Should().Be(IntegrationExecutionStatus.Succeeded);
                var captured = calls.ToArray();
                captured.Select(call => call.Path).Should().Equal("/first", "/first", "/second");
                captured.Select(call => call.Method).Should().Equal("PUT", "PUT", "PATCH");
                captured[0].Key.Should().NotBeNullOrWhiteSpace();
                captured[1].Key.Should().Be(captured[0].Key);
                captured[2].Key.Should().NotBe(captured[0].Key);
                captured.Select(call => call.Credential).Should().Equal("first-test-key", "first-test-key", "second-test-key");
                captured.Should().OnlyContain(call => call.Body.Contains("\"amount\":42"));

                await using var verify = new EnterpriseIntegrationHubDbContext(dbOptions);
                (await verify.EventReceipts.CountAsync()).Should().Be(1);
                (await verify.IntegrationExecutions.CountAsync()).Should().Be(1);
                (await verify.ExecutionDispatchOutbox.CountAsync()).Should().Be(1);
                (await verify.ExecutionDispatchInbox.SingleAsync()).CompletedAt.Should().NotBeNull();
                var attempts = await verify.ExecutionStepAttempts.OrderBy(attempt => attempt.StartedAt).ToListAsync();
                attempts.Select(attempt => attempt.HttpStatusCode).Should().Equal(503, 200, 200);
                attempts.Should().OnlyContain(attempt => attempt.DurationMilliseconds >= 0);

                using var observer = new ConsumerBuilder<string, string>(new ConsumerConfig
                {
                    BootstrapServers = kafka,
                    GroupId = group,
                    EnableAutoCommit = false
                }).Build();
                var committed = false;
                while (DateTimeOffset.UtcNow < deadline && !committed)
                {
                    committed = observer.Committed([new TopicPartition(topic, 0)], TimeSpan.FromSeconds(5)).Single().Offset.Value == 1;
                    if (!committed)
                        await Task.Delay(250);
                }
                committed.Should().BeTrue();

                using var recovery = await SubmitAsync("e2e-interrupted", 99);
                recovery.StatusCode.Should().Be(HttpStatusCode.Accepted);
                using var recoveryBody = JsonDocument.Parse(await recovery.Content.ReadAsStringAsync());
                recoveryExecutionId = recoveryBody.RootElement.GetProperty("executionId").GetGuid();
                await interruptedRequest.Task.WaitAsync(TimeSpan.FromSeconds(60));
            }

            await using (var expire = new EnterpriseIntegrationHubDbContext(dbOptions))
            {
                var updated = await expire.Database.ExecuteSqlInterpolatedAsync($"""
                    UPDATE [ExecutionDispatchInbox]
                    SET [LeaseExpiresAt] = DATEADD(second, -1, SYSDATETIMEOFFSET())
                    WHERE [IntegrationExecutionId] = {recoveryExecutionId} AND [ActiveAttemptId] IS NOT NULL
                    """);
                updated.Should().Be(1);
            }
            using (var restarted = CreateFactory())
            using (var client = restarted.CreateClient())
            {
                var deadline = DateTimeOffset.UtcNow.AddSeconds(90);
                IntegrationExecutionStatus? status = null;
                while (DateTimeOffset.UtcNow < deadline && status != IntegrationExecutionStatus.Succeeded)
                {
                    await using var poll = new EnterpriseIntegrationHubDbContext(dbOptions);
                    status = await poll.IntegrationExecutions.Where(execution => execution.Id == recoveryExecutionId)
                        .Select(execution => (IntegrationExecutionStatus?)execution.Status).SingleAsync();
                    if (status != IntegrationExecutionStatus.Succeeded)
                        await Task.Delay(250);
                }
                status.Should().Be(IntegrationExecutionStatus.Succeeded);
                var recoveryCalls = calls.Where(call => call.Body.Contains("\"amount\":99")).ToArray();
                recoveryCalls.Select(call => call.Path).Should().Equal("/first", "/first", "/second");
                recoveryCalls.Select(call => call.Method).Should().Equal("PUT", "PUT", "PATCH");
                recoveryCalls[0].Key.Should().Be(recoveryCalls[1].Key);
                await using var verify = new EnterpriseIntegrationHubDbContext(dbOptions);
                var recoverySteps = await verify.ExecutionSteps.Where(step => step.IntegrationExecutionId == recoveryExecutionId)
                    .Select(step => step.Id).ToListAsync();
                var recoveryAttempts = await verify.ExecutionStepAttempts.Where(attempt => recoverySteps.Contains(attempt.ExecutionStepId))
                    .OrderBy(attempt => attempt.StartedAt).ToListAsync();
                recoveryAttempts.Should().HaveCount(3);
                recoveryAttempts[0].FailureCode.Should().Be("INTERRUPTED");
                recoveryAttempts[1].HttpStatusCode.Should().Be(200);
                recoveryAttempts[2].HttpStatusCode.Should().Be(200);
            }
            await mock.StopAsync();
        }
        finally
        {
            await admin.DeleteTopicsAsync([topic]);
            await using var connection = new SqlConnection(masterConnection);
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = $"IF DB_ID('{databaseName}') IS NOT NULL BEGIN ALTER DATABASE [{databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{databaseName}]; END";
            await command.ExecuteNonQueryAsync();
        }
    }

    private sealed class DeliveryIntegrationFactAttribute : FactAttribute
    {
        public DeliveryIntegrationFactAttribute()
        {
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("EIH_SQLSERVER_TEST_CONNECTION")) ||
                string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("EIH_KAFKA_TEST_BOOTSTRAP")))
                Skip = "Set EIH_SQLSERVER_TEST_CONNECTION and EIH_KAFKA_TEST_BOOTSTRAP for end-to-end delivery tests.";
        }
    }
}
