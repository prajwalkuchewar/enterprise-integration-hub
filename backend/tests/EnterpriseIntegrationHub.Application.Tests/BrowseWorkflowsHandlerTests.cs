using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnterpriseIntegrationHub.Application.Features.Workflows.Browse;
using EnterpriseIntegrationHub.Application.Interfaces;
using EnterpriseIntegrationHub.Domain.Entities;
using FluentAssertions;
using Moq;
using Xunit;

namespace EnterpriseIntegrationHub.Application.Tests;

public class BrowseWorkflowsHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsWorkflowSummariesAndCount()
    {
        var first = new Workflow("Employee routing", Guid.NewGuid(), "EmployeeCreated", [new(Guid.NewGuid(), 1)]);
        var second = new Workflow("Order routing", Guid.NewGuid(), "OrderCreated", [new(Guid.NewGuid(), 1), new(Guid.NewGuid(), 2)]);
        var repository = new Mock<IWorkflowRepository>();
        repository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([first, second]);

        var response = await new BrowseWorkflowsHandler(repository.Object).Handle(new BrowseWorkflowsQuery(), CancellationToken.None);

        response.TotalCount.Should().Be(2);
        response.Items.Select(x => x.Name).Should().Equal("Employee routing", "Order routing");
        response.Items.Single(x => x.Id == second.Id).StepCount.Should().Be(2);
    }
}
