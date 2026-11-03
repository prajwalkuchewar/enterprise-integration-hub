using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using EnterpriseIntegrationHub.Application.Security;
using EnterpriseIntegrationHub.Domain.Entities;
using EnterpriseIntegrationHub.Domain.Enums;
using EnterpriseIntegrationHub.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EnterpriseIntegrationHub.Api.Tests;

public sealed class EventIngestionApiTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task Ingest_PersistsPayloadAndDoesNotReplaceItOnDuplicateOrConflict()
    {
        using var factory = new EnterpriseIntegrationHubApiFactory();
        using var client = factory.CreateClient();
        const string key = "eih_payload_test_key";
        const string sensitiveValue = "private-business-value";

        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<EnterpriseIntegrationHubDbContext>();
            var system = new ExternalSystem("Orders", "Test system", ExternalSystemEnvironment.Testing, ExternalSystemStatus.Active);
            var source = new Connector("Source", "Test source", system.Id, "https://source.example.test",
                ConnectorProtocol.REST, ConnectorAuthenticationType.APIKey, 30, ConnectorStatus.Active);
            source.AssignIngestionKeyHash(IngestionKeys.Hash(key));
            var destination = new Connector("Destination", "Test destination", system.Id, "https://destination.example.test",
                ConnectorProtocol.REST, ConnectorAuthenticationType.APIKey, 30, ConnectorStatus.Active);
            var workflow = new Workflow("Test workflow", source.Id, "order.created", [new(destination.Id, 1)]);
            workflow.Activate();
            context.ExternalSystems.Add(system);
            context.Connectors.AddRange(source, destination);
            context.Workflows.Add(workflow);
            context.WorkflowRevisions.Add(new WorkflowRevision(workflow));
            await context.SaveChangesAsync();
        }

        async Task<HttpResponseMessage> SubmitAsync(string value)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/events")
            {
                Content = new StringContent(JsonSerializer.Serialize(new
                {
                    eventId = "evt-payload",
                    eventType = "order.created",
                    sourceSystem = "Orders",
                    occurredAt = "2026-01-15T10:30:00Z",
                    correlationId = "corr-payload",
                    schemaVersion = 3,
                    payload = new { nested = new { value } }
                }, JsonOptions), Encoding.UTF8, "application/json")
            };
            request.Headers.Add("X-Ingestion-Key", key);
            return await client.SendAsync(request);
        }

        using var accepted = await SubmitAsync(sensitiveValue);
        accepted.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var acceptedBody = await accepted.Content.ReadAsStringAsync();
        acceptedBody.Should().NotContain(sensitiveValue);
        using var acceptedJson = JsonDocument.Parse(acceptedBody);
        var executionId = acceptedJson.RootElement.GetProperty("executionId").GetGuid();

        using var duplicate = await SubmitAsync(sensitiveValue);
        duplicate.StatusCode.Should().Be(HttpStatusCode.OK);
        using var conflict = await SubmitAsync("different-value");
        conflict.StatusCode.Should().Be(HttpStatusCode.Conflict);

        using var verifyScope = factory.Services.CreateScope();
        var verify = verifyScope.ServiceProvider.GetRequiredService<EnterpriseIntegrationHubDbContext>();
        var receipt = await verify.EventReceipts.SingleAsync();
        receipt.IntegrationExecutionId.Should().Be(executionId);
        receipt.SchemaVersion.Should().Be(3);
        using var payload = JsonDocument.Parse(receipt.PayloadJson!);
        payload.RootElement.GetProperty("nested").GetProperty("value").GetString().Should().Be(sensitiveValue);
        (await verify.IntegrationExecutions.CountAsync()).Should().Be(1);
        (await verify.ExecutionDispatchOutbox.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Ingest_WhenKeyBelongsToDifferentSourceSystem_ReturnsForbidden()
    {
        using var factory = new EnterpriseIntegrationHubApiFactory();
        using var client = factory.CreateClient();
        var connectorId = await CreateConnectorForSourceAsync(client, "Billing");
        var ingestionKey = await IssueIngestionKeyAsync(client, connectorId);

        using var response = await PostEventAsync(client, ingestionKey, sourceSystem: "Orders");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Ingest_WhenKeyIsUnknown_ReturnsUnauthorized()
    {
        using var factory = new EnterpriseIntegrationHubApiFactory();
        using var client = factory.CreateClient();

        using var response = await PostEventAsync(client, "eih_not_a_real_key", sourceSystem: "Orders");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Ingest_WhenKeyHeaderIsMissing_ReturnsUnauthorized()
    {
        using var factory = new EnterpriseIntegrationHubApiFactory();
        using var client = factory.CreateClient();

        using var response = await PostEventAsync(client, ingestionKey: null, sourceSystem: "Orders");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private static async Task<Guid> CreateConnectorForSourceAsync(HttpClient client, string externalSystemName)
    {
        using var externalSystem = await PostJsonAsync(client, "/api/external-systems",
            new { name = externalSystemName, description = "Ingestion API test system", environment = 1 });
        externalSystem.StatusCode.Should().Be(HttpStatusCode.Created);
        using var externalSystemBody = JsonDocument.Parse(await externalSystem.Content.ReadAsStringAsync());
        var externalSystemId = externalSystemBody.RootElement.GetProperty("id").GetGuid();

        using var connector = await PostJsonAsync(client, "/api/connectors", new
        {
            name = $"{externalSystemName} connector",
            description = "Ingestion API test connector",
            externalSystemId,
            baseUrl = "https://connector.example.test",
            protocol = 1,
            authenticationType = 1,
            timeoutSeconds = 30
        });
        connector.StatusCode.Should().Be(HttpStatusCode.Created);
        using var connectorBody = JsonDocument.Parse(await connector.Content.ReadAsStringAsync());
        return connectorBody.RootElement.GetProperty("id").GetGuid();
    }

    private static async Task<string> IssueIngestionKeyAsync(HttpClient client, Guid connectorId)
    {
        using var response = await client.PostAsync($"/api/connectors/{connectorId}/ingestion-key", null);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return body.RootElement.GetProperty("ingestionKey").GetString()!;
    }

    private static Task<HttpResponseMessage> PostEventAsync(HttpClient client, string? ingestionKey, string sourceSystem)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/events")
        {
            Content = new StringContent(JsonSerializer.Serialize(new
            {
                eventId = "evt-api-1",
                eventType = "order.created",
                sourceSystem,
                occurredAt = DateTimeOffset.UtcNow,
                schemaVersion = 1,
                payload = new { amount = 10 }
            }, JsonOptions), Encoding.UTF8, "application/json")
        };
        if (ingestionKey is not null)
            request.Headers.Add("X-Ingestion-Key", ingestionKey);

        return client.SendAsync(request);
    }

    private static Task<HttpResponseMessage> PostJsonAsync(HttpClient client, string url, object payload) =>
        client.PostAsync(url, new StringContent(JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8, "application/json"));
}
