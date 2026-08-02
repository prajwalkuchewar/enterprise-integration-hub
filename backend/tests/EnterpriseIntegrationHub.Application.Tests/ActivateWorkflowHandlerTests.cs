using System;
using System.Threading;
using System.Threading.Tasks;
using EnterpriseIntegrationHub.Application.Features.Workflows.Activate;
using EnterpriseIntegrationHub.Application.Interfaces;
using EnterpriseIntegrationHub.Domain.Entities;
using EnterpriseIntegrationHub.Domain.Enums;
using FluentAssertions;
using Moq;
using Xunit;

namespace EnterpriseIntegrationHub.Application.Tests;

public class ActivateWorkflowHandlerTests
{
    [Fact]
    public async Task Handle_WithActiveConnectors_ActivatesDraftWorkflow()
    {
        var sourceId = Guid.NewGuid();
        var destinationId = Guid.NewGuid();
        var workflow = new Workflow("Order routing", sourceId, "OrderCreated", [new(destinationId, 1)]);
        var workflows = new Mock<IWorkflowRepository>();
        var connectors = new Mock<IConnectorRepository>();
        workflows.Setup(x => x.GetByIdAsync(workflow.Id, It.IsAny<CancellationToken>())).ReturnsAsync(workflow);
        connectors.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(ActiveConnector());

        await new ActivateWorkflowHandler(workflows.Object, connectors.Object).Handle(new(workflow.Id), CancellationToken.None);

        workflow.Status.Should().Be(WorkflowStatus.Active);
        workflows.Verify(x => x.UpdateAsync(workflow, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenDestinationIsInactive_ThrowsConflict()
    {
        var sourceId = Guid.NewGuid();
        var destinationId = Guid.NewGuid();
        var workflow = new Workflow("Order routing", sourceId, "OrderCreated", [new(destinationId, 1)]);
        var workflows = new Mock<IWorkflowRepository>();
        var connectors = new Mock<IConnectorRepository>();
        workflows.Setup(x => x.GetByIdAsync(workflow.Id, It.IsAny<CancellationToken>())).ReturnsAsync(workflow);
        connectors.Setup(x => x.GetByIdAsync(sourceId, It.IsAny<CancellationToken>())).ReturnsAsync(ActiveConnector());
        connectors.Setup(x => x.GetByIdAsync(destinationId, It.IsAny<CancellationToken>())).ReturnsAsync(InactiveConnector());

        await Assert.ThrowsAsync<InvalidOperationException>(() => new ActivateWorkflowHandler(workflows.Object, connectors.Object).Handle(new(workflow.Id), CancellationToken.None));
        workflow.Status.Should().Be(WorkflowStatus.Draft);
    }

    private static Connector ActiveConnector() => CreateConnector(ConnectorStatus.Active);
    private static Connector InactiveConnector() => CreateConnector(ConnectorStatus.Draft);
    private static Connector CreateConnector(ConnectorStatus status) => new("Connector", "Description", Guid.NewGuid(), "https://example.com", ConnectorProtocol.REST, ConnectorAuthenticationType.APIKey, 30, status);
}
