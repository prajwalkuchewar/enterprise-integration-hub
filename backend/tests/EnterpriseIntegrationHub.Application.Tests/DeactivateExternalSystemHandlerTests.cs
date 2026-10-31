using System;
using System.Threading;
using System.Threading.Tasks;
using EnterpriseIntegrationHub.Application.Features.ExternalSystems.Deactivate;
using EnterpriseIntegrationHub.Application.Interfaces;
using EnterpriseIntegrationHub.Domain.Entities;
using EnterpriseIntegrationHub.Domain.Enums;
using FluentAssertions;
using Moq;
using Xunit;

namespace EnterpriseIntegrationHub.Application.Tests;

public sealed class DeactivateExternalSystemHandlerTests
{
    [Fact]
    public async Task Handle_WhenSystemHasActiveConnector_ThrowsConflictAndDoesNotPersist()
    {
        var system = CreateActiveSystem();
        var systems = new Mock<IExternalSystemRepository>();
        var connectors = new Mock<IConnectorRepository>();
        var workflows = new Mock<IWorkflowRepository>();
        systems.Setup(x => x.GetByIdAsync(system.Id, It.IsAny<CancellationToken>())).ReturnsAsync(system);
        connectors.Setup(x => x.HasActiveConnectorForExternalSystemAsync(system.Id, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var action = () => CreateHandler(systems, connectors, workflows)
            .Handle(new DeactivateExternalSystemCommand(system.Id), CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*active connectors*");
        system.Status.Should().Be(ExternalSystemStatus.Active);
        systems.Verify(x => x.UpdateAsync(It.IsAny<ExternalSystem>(), It.IsAny<CancellationToken>()), Times.Never);
        workflows.Verify(x => x.HasActiveWorkflowForExternalSystemAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenActiveWorkflowDependsOnSystem_ThrowsConflictAndDoesNotPersist()
    {
        var system = CreateActiveSystem();
        var systems = new Mock<IExternalSystemRepository>();
        var connectors = new Mock<IConnectorRepository>();
        var workflows = new Mock<IWorkflowRepository>();
        systems.Setup(x => x.GetByIdAsync(system.Id, It.IsAny<CancellationToken>())).ReturnsAsync(system);
        connectors.Setup(x => x.HasActiveConnectorForExternalSystemAsync(system.Id, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        workflows.Setup(x => x.HasActiveWorkflowForExternalSystemAsync(system.Id, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var action = () => CreateHandler(systems, connectors, workflows)
            .Handle(new DeactivateExternalSystemCommand(system.Id), CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*active workflow depends on it*");
        systems.Verify(x => x.UpdateAsync(It.IsAny<ExternalSystem>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenNoActiveDependencyExists_DeactivatesAndPersists()
    {
        var system = CreateActiveSystem();
        var systems = new Mock<IExternalSystemRepository>();
        var connectors = new Mock<IConnectorRepository>();
        var workflows = new Mock<IWorkflowRepository>();
        systems.Setup(x => x.GetByIdAsync(system.Id, It.IsAny<CancellationToken>())).ReturnsAsync(system);
        connectors.Setup(x => x.HasActiveConnectorForExternalSystemAsync(system.Id, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        workflows.Setup(x => x.HasActiveWorkflowForExternalSystemAsync(system.Id, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        await CreateHandler(systems, connectors, workflows)
            .Handle(new DeactivateExternalSystemCommand(system.Id), CancellationToken.None);

        system.Status.Should().Be(ExternalSystemStatus.Inactive);
        systems.Verify(x => x.UpdateAsync(system, It.IsAny<CancellationToken>()), Times.Once);
    }

    private static DeactivateExternalSystemHandler CreateHandler(
        Mock<IExternalSystemRepository> systems,
        Mock<IConnectorRepository> connectors,
        Mock<IWorkflowRepository> workflows) =>
        new(systems.Object, connectors.Object, workflows.Object);

    private static ExternalSystem CreateActiveSystem() =>
        new("Orders", "Orders system", ExternalSystemEnvironment.Production, ExternalSystemStatus.Active);
}

