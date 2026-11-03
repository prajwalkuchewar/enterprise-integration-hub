using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EnterpriseIntegrationHub.Application.Features.Events.Ingest;
using EnterpriseIntegrationHub.Application.Features.Executions.Create;
using EnterpriseIntegrationHub.Application.Features.Workflows.Resolve;
using EnterpriseIntegrationHub.Application.Interfaces;
using EnterpriseIntegrationHub.Application.Security;
using EnterpriseIntegrationHub.Domain.Entities;
using EnterpriseIntegrationHub.Domain.Enums;
using FluentAssertions;
using Moq;
using Xunit;

namespace EnterpriseIntegrationHub.Application.Tests;

public class IngestEventHandlerTests
{
    private const string ValidKey = "eih_test_key";

    private readonly Mock<IConnectorRepository> _connectorRepository = new();
    private readonly Mock<IExternalSystemRepository> _externalSystemRepository = new();
    private readonly IngestEventHandler _handler;

    public IngestEventHandlerTests()
    {
        var workflowRepository = new Mock<IWorkflowRepository>();
        var executionRepository = new Mock<IIntegrationExecutionRepository>();
        var resolver = new ResolveWorkflowHandler(workflowRepository.Object, _connectorRepository.Object);
        var createExecution = new CreateWorkflowExecutionHandler(resolver, workflowRepository.Object, executionRepository.Object);

        _handler = new IngestEventHandler(_connectorRepository.Object, _externalSystemRepository.Object, createExecution);
    }

    private static IngestEventCommand CreateCommand(string ingestionKey = ValidKey, string sourceSystem = "Orders") =>
        new(ingestionKey, "evt-1", "order.created", sourceSystem, DateTimeOffset.UtcNow, null, 1,
            JsonDocument.Parse("{\"amount\":10}").RootElement.Clone());

    private void SetupConnector(Guid externalSystemId, ConnectorStatus status)
    {
        var connector = new Connector("Test Connector", "Description", externalSystemId, "https://api.example.com",
            ConnectorProtocol.REST, ConnectorAuthenticationType.APIKey, 30, status);
        connector.AssignIngestionKeyHash(IngestionKeys.Hash(ValidKey));

        _connectorRepository
            .Setup(r => r.GetByIngestionKeyHashAsync(IngestionKeys.Hash(ValidKey), It.IsAny<CancellationToken>()))
            .ReturnsAsync(connector);
    }

    private void SetupExternalSystem(Guid externalSystemId, string name)
    {
        _externalSystemRepository
            .Setup(r => r.GetByIdAsync(externalSystemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExternalSystem(name, "Description", ExternalSystemEnvironment.Development, ExternalSystemStatus.Active));
    }

    [Fact]
    public async Task Handle_WhenKeyMissing_ThrowsUnauthorized()
    {
        var act = () => _handler.Handle(CreateCommand(ingestionKey: "  "), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Handle_WhenKeyUnknown_ThrowsUnauthorized()
    {
        _connectorRepository
            .Setup(r => r.GetByIngestionKeyHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Connector?)null);

        var act = () => _handler.Handle(CreateCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Handle_WhenSourceSystemDoesNotMatchKeyOwner_ThrowsForbidden()
    {
        var externalSystemId = Guid.NewGuid();
        SetupConnector(externalSystemId, ConnectorStatus.Active);
        SetupExternalSystem(externalSystemId, "Billing");

        var act = () => _handler.Handle(CreateCommand(sourceSystem: "Orders"), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task Handle_WhenConnectorInactive_ThrowsInvalidOperation()
    {
        var externalSystemId = Guid.NewGuid();
        SetupConnector(externalSystemId, ConnectorStatus.Draft);
        SetupExternalSystem(externalSystemId, "Orders");

        var act = () => _handler.Handle(CreateCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_WhenEventIdBlank_ThrowsArgumentException(string eventId)
    {
        var command = new IngestEventCommand(ValidKey, eventId, "order.created", "Orders", DateTimeOffset.UtcNow, null, 1,
            JsonDocument.Parse("{}").RootElement.Clone());

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Handle_WhenPayloadNotObject_ThrowsArgumentException()
    {
        var command = new IngestEventCommand(ValidKey, "evt-1", "order.created", "Orders", DateTimeOffset.UtcNow, null, 1,
            JsonDocument.Parse("[1,2]").RootElement.Clone());

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Handle_WhenSchemaVersionNotPositive_ThrowsArgumentException()
    {
        var command = new IngestEventCommand(ValidKey, "evt-1", "order.created", "Orders", DateTimeOffset.UtcNow, null, 0,
            JsonDocument.Parse("{}").RootElement.Clone());

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
    }
}
