using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace EnterpriseIntegrationHub.Api.Tests;

public sealed class ApiHostIntegrationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task GetExternalSystems_ResolvesDependenciesAndReturnsEmptyList()
    {
        using var factory = new EnterpriseIntegrationHubApiFactory();
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/api/external-systems");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        body.RootElement.GetProperty("items").GetArrayLength().Should().Be(0);
        body.RootElement.GetProperty("totalCount").GetInt32().Should().Be(0);
    }

    [Fact]
    public async Task CreateConnector_WithEmptyExternalSystemId_IsRejectedByRequestValidation()
    {
        using var factory = new EnterpriseIntegrationHubApiFactory();
        using var client = factory.CreateClient();
        var payload = new { name = "Orders", description = "Orders endpoint", externalSystemId = Guid.Empty, baseUrl = "https://orders.example.test", protocol = 1, authenticationType = 1, timeoutSeconds = 30 };
        using var response = await PostJsonAsync(client, "/api/connectors", payload);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ActiveWorkflow_PreventsConnectorDeactivationThroughHttpApi()
    {
        using var factory = new EnterpriseIntegrationHubApiFactory();
        using var client = factory.CreateClient();
        var externalSystemId = await CreateExternalSystemAsync(client, "Orders");
        var sourceConnectorId = await CreateConnectorAsync(client, externalSystemId, "Orders source");
        var destinationConnectorId = await CreateConnectorAsync(client, externalSystemId, "Orders destination");

        (await client.PostAsync($"/api/connectors/{sourceConnectorId}/activate", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await client.PostAsync($"/api/connectors/{destinationConnectorId}/activate", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var createWorkflow = await PostJsonAsync(client, "/api/workflows", new
        {
            name = "Order routing",
            sourceConnectorId,
            triggerEvent = "OrderCreated",
            steps = new[] { new { destinationConnectorId, executionOrder = 1 } }
        });
        createWorkflow.StatusCode.Should().Be(HttpStatusCode.Created);
        using var workflowBody = JsonDocument.Parse(await createWorkflow.Content.ReadAsStringAsync());
        var workflowId = workflowBody.RootElement.GetProperty("id").GetGuid();

        (await client.PostAsync($"/api/workflows/{workflowId}/activate", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        using var deactivate = await client.PostAsync($"/api/connectors/{destinationConnectorId}/deactivate", null);
        deactivate.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    private static async Task<Guid> CreateExternalSystemAsync(HttpClient client, string name)
    {
        using var response = await PostJsonAsync(client, "/api/external-systems", new { name, description = "API host test system", environment = 1 });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return body.RootElement.GetProperty("id").GetGuid();
    }

    private static async Task<Guid> CreateConnectorAsync(HttpClient client, Guid externalSystemId, string name)
    {
        using var response = await PostJsonAsync(client, "/api/connectors", new
        {
            name,
            description = "API host test connector",
            externalSystemId,
            baseUrl = "https://connector.example.test",
            protocol = 1,
            authenticationType = 1,
            timeoutSeconds = 30
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return body.RootElement.GetProperty("id").GetGuid();
    }

    private static Task<HttpResponseMessage> PostJsonAsync(HttpClient client, string url, object payload) =>
        client.PostAsync(url, new StringContent(JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8, "application/json"));
}

