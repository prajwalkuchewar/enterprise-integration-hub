using EnterpriseIntegrationHub.Application.Features.Workflows.Create;
using EnterpriseIntegrationHub.Application.Interfaces;
using EnterpriseIntegrationHub.Domain.Entities;
using EnterpriseIntegrationHub.Domain.Enums;
using FluentAssertions;
using Moq;

namespace EnterpriseIntegrationHub.Application.Tests;

public class CreateWorkflowHandlerTests
{
    private readonly Mock<IWorkflowRepository> _workflows = new();
    private readonly Mock<IConnectorRepository> _connectors = new();
    private readonly CreateWorkflowHandler _handler;

    public CreateWorkflowHandlerTests() => _handler = new(_workflows.Object, _connectors.Object);

    [Fact]
    public async Task Handle_WithActiveSourceAndDestinations_PersistsDraftWorkflow()
    {
        var sourceId = Guid.NewGuid();
        var destinationId = Guid.NewGuid();
        SetupActive(sourceId);
        SetupActive(destinationId);
        _workflows.Setup(x => x.ExistsByNameAsync("Order routing", It.IsAny<CancellationToken>())).ReturnsAsync(false);
        Workflow? persisted = null;
        _workflows.Setup(x => x.AddAsync(It.IsAny<Workflow>(), It.IsAny<CancellationToken>()))
            .Callback<Workflow, CancellationToken>((workflow, _) => persisted = workflow)
            .Returns(Task.CompletedTask);

        var id = await _handler.Handle(new("Order routing", sourceId, [new(destinationId, 1)], "OrderCreated"), CancellationToken.None);

        id.Should().NotBeEmpty();
        persisted!.Status.Should().Be(WorkflowStatus.Draft);
        persisted.Steps.Select(x => x.DestinationConnectorId).Should().ContainSingle().Which.Should().Be(destinationId);
    }

    [Fact]
    public async Task Handle_WhenSourceDoesNotExist_ThrowsNotFound()
    {
        var sourceId = Guid.NewGuid();
        _workflows.Setup(x => x.ExistsByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _connectors.Setup(x => x.GetByIdAsync(sourceId, It.IsAny<CancellationToken>())).ReturnsAsync((Connector?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => _handler.Handle(new("Order routing", sourceId, [new(Guid.NewGuid(), 1)], "OrderCreated"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenDestinationIsInactive_ThrowsConflict()
    {
        var sourceId = Guid.NewGuid();
        var destinationId = Guid.NewGuid();
        SetupActive(sourceId);
        _workflows.Setup(x => x.ExistsByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _connectors.Setup(x => x.GetByIdAsync(destinationId, It.IsAny<CancellationToken>())).ReturnsAsync(CreateConnector(ConnectorStatus.Draft));

        await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.Handle(new("Order routing", sourceId, [new(destinationId, 1)], "OrderCreated"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenNameAlreadyExists_ThrowsConflict()
    {
        _workflows.Setup(x => x.ExistsByNameAsync("Order routing", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.Handle(new("Order routing", Guid.NewGuid(), [new(Guid.NewGuid(), 1)], "OrderCreated"), CancellationToken.None));
    }

    private void SetupActive(Guid id) => _connectors.Setup(x => x.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(CreateConnector(ConnectorStatus.Active));
    private static Connector CreateConnector(ConnectorStatus status) => new("Connector", "Description", Guid.NewGuid(), "https://example.com", ConnectorProtocol.REST, ConnectorAuthenticationType.APIKey, 30, status);
}
