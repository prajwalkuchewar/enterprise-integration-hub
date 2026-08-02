using EnterpriseIntegrationHub.Application.Features.Workflows.ViewDetails;
using EnterpriseIntegrationHub.Application.Interfaces;
using EnterpriseIntegrationHub.Domain.Entities;
using FluentAssertions;
using Moq;

namespace EnterpriseIntegrationHub.Application.Tests;

public class ViewWorkflowDetailsHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsWorkflowWithStepsInExecutionOrder()
    {
        var workflow = new Workflow("Order routing", Guid.NewGuid(), "OrderCreated", [new(Guid.NewGuid(), 2), new(Guid.NewGuid(), 1)]);
        var repository = new Mock<IWorkflowRepository>();
        repository.Setup(x => x.GetByIdAsync(workflow.Id, It.IsAny<CancellationToken>())).ReturnsAsync(workflow);

        var response = await new ViewWorkflowDetailsHandler(repository.Object).Handle(new(workflow.Id), CancellationToken.None);

        response.Name.Should().Be("Order routing");
        response.Steps.Select(x => x.ExecutionOrder).Should().Equal(1, 2);
    }

    [Fact]
    public async Task Handle_WhenWorkflowDoesNotExist_ThrowsNotFound()
    {
        var repository = new Mock<IWorkflowRepository>();
        repository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Workflow?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => new ViewWorkflowDetailsHandler(repository.Object).Handle(new(Guid.NewGuid()), CancellationToken.None));
    }
}
