using EnterpriseIntegrationHub.Application.Features.Workflows.Update;
using EnterpriseIntegrationHub.Application.Interfaces;
using EnterpriseIntegrationHub.Domain.Entities;
using EnterpriseIntegrationHub.Domain.Enums;
using FluentAssertions;
using Moq;

namespace EnterpriseIntegrationHub.Application.Tests;

public class UpdateWorkflowHandlerTests
{
    [Fact]
    public async Task Handle_WithValidDraftWorkflow_ReplacesItsDetailsAndSteps()
    {
        var workflow = new Workflow("Old routing", Guid.NewGuid(), "OldEvent", [new(Guid.NewGuid(), 1)]);
        var sourceId = Guid.NewGuid();
        var destinationId = Guid.NewGuid();
        var workflows = new Mock<IWorkflowRepository>();
        var connectors = new Mock<IConnectorRepository>();
        workflows.Setup(x => x.GetByIdAsync(workflow.Id, It.IsAny<CancellationToken>())).ReturnsAsync(workflow);
        workflows.Setup(x => x.ExistsByNameAsync("New routing", It.IsAny<CancellationToken>(), workflow.Id)).ReturnsAsync(false);
        connectors.Setup(x => x.GetByIdAsync(sourceId, It.IsAny<CancellationToken>())).ReturnsAsync(ActiveConnector());
        connectors.Setup(x => x.GetByIdAsync(destinationId, It.IsAny<CancellationToken>())).ReturnsAsync(ActiveConnector());

        await new UpdateWorkflowHandler(workflows.Object, connectors.Object).Handle(workflow.Id,
            new("New routing", sourceId, [new(destinationId, 1)], "NewEvent"), CancellationToken.None);

        workflow.Name.Should().Be("New routing");
        workflow.TriggerEvent.Should().Be("NewEvent");
        workflow.Steps.Should().ContainSingle(x => x.DestinationConnectorId == destinationId);
        workflows.Verify(x => x.UpdateAsync(workflow, It.IsAny<CancellationToken>()), Times.Once);
    }

    private static Connector ActiveConnector() => new("Connector", "Description", Guid.NewGuid(), "https://example.com", ConnectorProtocol.REST, ConnectorAuthenticationType.APIKey, 30, ConnectorStatus.Active);
}
