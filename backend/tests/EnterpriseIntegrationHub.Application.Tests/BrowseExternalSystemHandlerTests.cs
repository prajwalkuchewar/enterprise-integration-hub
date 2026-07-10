using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnterpriseIntegrationHub.Application.Features.ExternalSystems.Browse;
using EnterpriseIntegrationHub.Application.Interfaces;
using EnterpriseIntegrationHub.Domain.Entities;
using EnterpriseIntegrationHub.Domain.Enums;
using FluentAssertions;
using Moq;
using Xunit;

namespace EnterpriseIntegrationHub.Application.Tests;

public class BrowseExternalSystemsHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsMappedSummariesAndTotalCount()
    {
        var items = new List<ExternalSystem>
        {
            new ExternalSystem("System A", "Description A", ExternalSystemEnvironment.Development, ExternalSystemStatus.Active),
            new ExternalSystem("System B", "Description B", ExternalSystemEnvironment.Testing, ExternalSystemStatus.Inactive)
        };

        var repo = new Mock<IExternalSystemRepository>();
        repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(items.AsReadOnly());

        var handler = new BrowseExternalSystemsHandler(repo.Object);
        var query = new BrowseExternalSystemsQuery();

        var result = await handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.TotalCount.Should().Be(items.Count);
        result.Items.Should().HaveCount(items.Count);

        var resultNames = result.Items.Select(i => i.Name).ToList();
        var expectedNames = items.Select(x => x.Name).OrderBy(x => x).ToList();
        resultNames.Should().BeEquivalentTo(expectedNames);
    }

    [Fact]
    public async Task Handle_WhenNoSystems_ReturnsEmptyListAndZeroCount()
    {
        var repo = new Mock<IExternalSystemRepository>();
        repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ExternalSystem>().AsReadOnly());

        var handler = new BrowseExternalSystemsHandler(repo.Object);
        var query = new BrowseExternalSystemsQuery();

        var result = await handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.TotalCount.Should().Be(0);
        result.Items.Should().BeEmpty();
    }
}
