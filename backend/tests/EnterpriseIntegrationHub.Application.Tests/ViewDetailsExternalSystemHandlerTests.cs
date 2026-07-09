using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnterpriseIntegrationHub.Application.Features.ExternalSystems.ViewDetails;
using EnterpriseIntegrationHub.Application.Interfaces;
using EnterpriseIntegrationHub.Domain.Entities;
using EnterpriseIntegrationHub.Domain.Enums;
using FluentAssertions;
using Moq;
using Xunit;

namespace EnterpriseIntegrationHub.Application.Tests;

public class ViewDetailsExternalSystemHandlerTests
{
    [Fact]
    public async Task Handle_WhenNotFound_ThrowsKeyNotFoundException()
    {
        var repo = new Mock<IExternalSystemRepository>();
        repo.Setup(r => r.GetDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ExternalSystem?)null);

        var handler = new ViewDetailsExternalSystemHandler(repo.Object);
        var query = new ViewDetailsExternalSystemQuery(Guid.NewGuid());

        await Assert.ThrowsAsync<KeyNotFoundException>(() => handler.Handle(query, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenFound_ReturnsMappedSummary()
    {
        var entity = new ExternalSystem("Name", "Description", ExternalSystemEnvironment.UserAcceptance, ExternalSystemStatus.Active);
        var repo = new Mock<IExternalSystemRepository>();
        repo.Setup(r => r.GetDetailsAsync(entity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        var handler = new ViewDetailsExternalSystemHandler(repo.Object);
        var query = new ViewDetailsExternalSystemQuery(entity.Id);

        var result = await handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be(entity.Id);
        result.Name.Should().Be(entity.Name);
        result.Description.Should().Be(entity.Description);
    }
}
