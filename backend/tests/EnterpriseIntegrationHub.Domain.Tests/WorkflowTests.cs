using EnterpriseIntegrationHub.Domain.Entities;
using EnterpriseIntegrationHub.Domain.Enums;
using FluentAssertions;

namespace EnterpriseIntegrationHub.Domain.Tests;

public class WorkflowTests
{
    [Fact]
    public void Constructor_WithValidValues_CreatesDraftWorkflowWithDestinations()
    {
        var sourceId = Guid.NewGuid();
        var destinationIds = new[] { Guid.NewGuid(), Guid.NewGuid() };
        var workflow = new Workflow("Order routing", sourceId, "OrderCreated", [new(destinationIds[0], 1), new(destinationIds[1], 2)]);

        workflow.Status.Should().Be(WorkflowStatus.Draft);
        workflow.SourceConnectorId.Should().Be(sourceId);
        workflow.TriggerEvent.Should().Be("OrderCreated");
        workflow.Steps.Select(x => x.DestinationConnectorId).Should().BeEquivalentTo(destinationIds);
    }

    [Fact]
    public void Constructor_WithoutName_Throws()
    {
        Action action = () => new Workflow(" ", Guid.NewGuid(), "OrderCreated", [new WorkflowStepDefinition(Guid.NewGuid(), 1)]);
        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_WithoutDestinations_Throws()
    {
        Action action = () => new Workflow("Order routing", Guid.NewGuid(), "OrderCreated", []);
        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_WhenSourceIsDestination_Throws()
    {
        var connectorId = Guid.NewGuid();
        Action action = () => new Workflow("Order routing", connectorId, "OrderCreated", [new WorkflowStepDefinition(connectorId, 1)]);
        action.Should().Throw<ArgumentException>();
    }
}
