using System.Net;
using System.Text;
using EnterpriseIntegrationHub.Api.Delivery;
using EnterpriseIntegrationHub.Application.Features.Executions.Worker;
using EnterpriseIntegrationHub.Application.Interfaces;
using EnterpriseIntegrationHub.Domain.Enums;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Xunit;

namespace EnterpriseIntegrationHub.Api.Tests;

public sealed class HttpExecutionDestinationInvokerTests
{
    [Theory]
    [InlineData("127.0.0.1")]
    [InlineData("10.2.3.4")]
    [InlineData("100.64.0.1")]
    [InlineData("169.254.169.254")]
    [InlineData("172.31.0.1")]
    [InlineData("192.168.1.1")]
    [InlineData("192.0.2.1")]
    [InlineData("198.19.0.1")]
    [InlineData("224.0.0.1")]
    [InlineData("::1")]
    [InlineData("::ffff:169.254.169.254")]
    [InlineData("fc00::1")]
    [InlineData("fe80::1")]
    [InlineData("2001:db8::1")]
    [InlineData("2002:0808:0808::1")]
    [InlineData("64:ff9b::a00:1")]
    public void NetworkPolicy_BlocksUnsafeAddresses(string address)
    {
        var policy = CreatePolicy(new DestinationHttpOptions());
        policy.IsAllowedAddress(IPAddress.Parse(address), true).Should().BeFalse();
    }

    [Theory]
    [InlineData("8.8.8.8")]
    [InlineData("1.1.1.1")]
    [InlineData("2606:4700:4700::1111")]
    public void NetworkPolicy_AllowsPublicAddresses(string address)
    {
        CreatePolicy(new DestinationHttpOptions()).IsAllowedAddress(IPAddress.Parse(address), false).Should().BeTrue();
    }

    [Fact]
    public void NetworkPolicy_RequiresExactOriginAndDisallowsUnsafeProductionOptions()
    {
        var options = new DestinationHttpOptions { AllowedOrigins = ["https://example.com:443"] };
        var policy = CreatePolicy(options);
        policy.IsAllowedDestination("https://example.com/path", out _).Should().BeTrue();
        foreach (var address in new[] { "https://example.com.evil.test/path", "https://example.com:444/path",
                     "https://user@example.com/path", "https://example.com/path?token=secret",
                     "https://example.com/path#fragment", "http://example.com/path" })
            policy.IsAllowedDestination(address, out _).Should().BeFalse();
        var action = () => CreatePolicy(new DestinationHttpOptions { AllowLoopback = true }, "Production");
        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public async Task Invoker_DeliversPostWithAuthenticationAndNeverFollowsRedirects()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.ConfigureKestrel(server => server.Listen(IPAddress.Loopback, 0));
        await using var mock = builder.Build();
        var requests = new List<(string Path, string? Authorization, string? ApiKey, string? Idempotency, string? Correlation, string Body)>();
        mock.MapPost("/{action}", async context =>
        {
            using var reader = new StreamReader(context.Request.Body);
            var body = await reader.ReadToEndAsync();
            requests.Add((context.Request.Path.Value!, context.Request.Headers.Authorization.FirstOrDefault(),
                context.Request.Headers["X-API-Key"].FirstOrDefault(), context.Request.Headers["Idempotency-Key"].FirstOrDefault(),
                context.Request.Headers["X-Correlation-ID"].FirstOrDefault(), body));
            switch (context.Request.RouteValues["action"]?.ToString())
            {
                case "redirect":
                    context.Response.StatusCode = 302;
                    context.Response.Headers.Location = "/unexpected";
                    break;
                case "retry": context.Response.StatusCode = 503; break;
                case "throttled": context.Response.StatusCode = 429; break;
                case "expired": context.Response.StatusCode = 408; break;
                case "bad": context.Response.StatusCode = 400; break;
                case "slow": await Task.Delay(TimeSpan.FromSeconds(4), context.RequestAborted); break;
            }
            await context.Response.WriteAsync("private response body");
        });
        await mock.StartAsync();
        var origin = mock.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        var port = new Uri(origin).Port;
        var ids = Enumerable.Range(0, 4).Select(_ => Guid.NewGuid()).ToArray();
        var config = new Dictionary<string, string?>
        {
            ["DestinationHttp:AllowedOrigins:0"] = origin,
            ["DestinationHttp:AllowedOrigins:1"] = $"http://localhost:{port}",
            ["DestinationHttp:AllowInsecureHttp"] = "true",
            ["DestinationHttp:AllowLoopback"] = "true",
            [$"DestinationCredentials:{ids[0]:D}:HeaderName"] = "X-API-Key",
            [$"DestinationCredentials:{ids[0]:D}:ApiKey"] = "secret-key",
            [$"DestinationCredentials:{ids[1]:D}:Username"] = "operator",
            [$"DestinationCredentials:{ids[1]:D}:Password"] = "secret-password",
            [$"DestinationCredentials:{ids[2]:D}:AccessToken"] = "secret-token",
            [$"DestinationCredentials:{ids[3]:D}:AccessToken"] = "oauth-token"
        };
        using var baseFactory = new EnterpriseIntegrationHubApiFactory();
        using var factory = baseFactory.WithWebHostBuilder(host =>
            host.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(config)));
        using var hostClient = factory.CreateClient();
        using var scope = factory.Services.CreateScope();
        var invoker = scope.ServiceProvider.GetRequiredService<IExecutionDestinationInvoker>();
        var step = Guid.NewGuid();
        ExecutionAttemptWork Work(int index, ConnectorAuthenticationType auth, string path, string? target = null) =>
            new(Guid.NewGuid(), step, Guid.NewGuid(), 1, 1, ids[index], $"{target ?? origin}/{path}",
                ConnectorProtocol.REST, auth, 2, "correlation-1", "{\"sensitive\":\"payload\"}");

        var keyResult = await invoker.InvokeAsync(Work(0, ConnectorAuthenticationType.APIKey, "ok"), default);
        keyResult.Status.Should().Be(DestinationInvocationStatus.Succeeded);
        keyResult.HttpStatusCode.Should().Be(200);
        keyResult.DurationMilliseconds.Should().BeGreaterThanOrEqualTo(0);
        requests[0].ApiKey.Should().Be("secret-key");
        requests[0].Idempotency.Should().Be(step.ToString("D"));
        requests[0].Correlation.Should().Be("correlation-1");
        requests[0].Body.Should().Be("{\"sensitive\":\"payload\"}");

        await invoker.InvokeAsync(Work(1, ConnectorAuthenticationType.Basic, "ok"), default);
        requests[1].Authorization.Should().Be("Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes("operator:secret-password")));
        await invoker.InvokeAsync(Work(2, ConnectorAuthenticationType.BearerToken, "ok"), default);
        requests[2].Authorization.Should().Be("Bearer secret-token");
        await invoker.InvokeAsync(Work(3, ConnectorAuthenticationType.OAuth2, "ok"), default);
        requests[3].Authorization.Should().Be("Bearer oauth-token");
        var retry = await invoker.InvokeAsync(Work(0, ConnectorAuthenticationType.APIKey, "retry"), default);
        retry.Status.Should().Be(DestinationInvocationStatus.TransientFailure);
        retry.HttpStatusCode.Should().Be(503);
        (await invoker.InvokeAsync(Work(0, ConnectorAuthenticationType.APIKey, "throttled"), default))
            .Status.Should().Be(DestinationInvocationStatus.TransientFailure);
        (await invoker.InvokeAsync(Work(0, ConnectorAuthenticationType.APIKey, "expired"), default))
            .Status.Should().Be(DestinationInvocationStatus.TransientFailure);
        var bad = await invoker.InvokeAsync(Work(0, ConnectorAuthenticationType.APIKey, "bad"), default);
        bad.Status.Should().Be(DestinationInvocationStatus.PermanentFailure);
        bad.HttpStatusCode.Should().Be(400);
        var redirect = await invoker.InvokeAsync(Work(0, ConnectorAuthenticationType.APIKey, "redirect"), default);
        redirect.Status.Should().Be(DestinationInvocationStatus.PermanentFailure);
        redirect.HttpStatusCode.Should().Be(302);
        requests.Should().NotContain(request => request.Path == "/unexpected");
        redirect.ToString().Should().NotContain("private response body");

        var blocked = await invoker.InvokeAsync(Work(0, ConnectorAuthenticationType.APIKey, "ok", $"http://localhost:{port}"), default);
        blocked.Status.Should().Be(DestinationInvocationStatus.InvalidConfiguration);
        requests.Should().HaveCount(9);
        var timeout = await invoker.InvokeAsync(Work(0, ConnectorAuthenticationType.APIKey, "slow") with { TimeoutSeconds = 1 }, default);
        timeout.Status.Should().Be(DestinationInvocationStatus.TransientFailure);
        timeout.HttpStatusCode.Should().BeNull();
        timeout.DurationMilliseconds.Should().BeGreaterThanOrEqualTo(900);

        var invalid = await invoker.InvokeAsync(Work(0, ConnectorAuthenticationType.APIKey, "ok") with { Protocol = ConnectorProtocol.SOAP }, default);
        invalid.Status.Should().Be(DestinationInvocationStatus.InvalidConfiguration);
        var invalidMethod = await invoker.InvokeAsync(Work(0, ConnectorAuthenticationType.APIKey, "ok") with
        {
            HttpMethod = (ConnectorHttpMethod)99
        }, default);
        invalidMethod.Status.Should().Be(DestinationInvocationStatus.InvalidConfiguration);
        var noCredential = await invoker.InvokeAsync(Work(0, ConnectorAuthenticationType.APIKey, "ok") with
        {
            DestinationConnectorId = Guid.NewGuid()
        }, default);
        noCredential.Status.Should().Be(DestinationInvocationStatus.InvalidConfiguration);
        var unsafeCorrelation = await invoker.InvokeAsync(Work(0, ConnectorAuthenticationType.APIKey, "ok") with
        {
            CorrelationId = "safe\r\nInjected: secret"
        }, default);
        unsafeCorrelation.Status.Should().Be(DestinationInvocationStatus.InvalidConfiguration);
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        var cancelledAction = async () => await invoker.InvokeAsync(Work(0, ConnectorAuthenticationType.APIKey, "ok"), cancelled.Token);
        await cancelledAction.Should().ThrowAsync<OperationCanceledException>();
        await mock.StopAsync();
    }

    private static DestinationNetworkPolicy CreatePolicy(DestinationHttpOptions options, string environment = "Testing") =>
        new(Options.Create(options), new Microsoft.Extensions.Hosting.Internal.HostingEnvironment { EnvironmentName = environment });
}
