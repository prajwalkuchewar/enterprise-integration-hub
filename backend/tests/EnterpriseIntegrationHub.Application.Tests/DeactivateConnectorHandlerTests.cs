using System;
using System.Threading;
using System.Threading.Tasks;
using EnterpriseIntegrationHub.Application.Features.Connectors.Deactivate;
using EnterpriseIntegrationHub.Application.Interfaces;
using EnterpriseIntegrationHub.Domain.Entities;
using EnterpriseIntegrationHub.Domain.Enums;
using FluentAssertions;
using Moq;
using Xunit;

namespace EnterpriseIntegrationHub.Application.Tests;

public sealed class DeactivateConnectorHandlerTests
{
    [Fact]
    public async Task Handle_WhenActiveWorkflowDependsOnConnector_ThrowsConflictAndDoesNotPersist()
    {
        var connector = CreateActiveConnector();
        var connectors = new Mock<IConnectorRepository>();
        var workflows = new Mock<IWorkflowRepository>();
        connectors.Setup(x => x.GetByIdAsync(connector.Id, It.IsAny<CancellationToken>())).ReturnsAsync(connector);
        workflows.Setup(x => x.HasActiveWorkflowForConnectorAsync(connector.Id, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var action = () => new DeactivateConnectorHandler(connectors.Object, workflows.Object)
            .Handle(new DeactivateConnectorCommand(connector.Id), CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*active workflow depends on it*");
        connector.Status.Should().Be(ConnectorStatus.Active);
        connectors.Verify(x => x.UpdateAsync(It.IsAny<Connector>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenNoActiveWorkflowDependsOnConnector_DeactivatesAndPersists()
    {
        var connector = CreateActiveConnector();
        var connectors = new Mock<IConnectorRepository>();
        var workflows = new Mock<IWorkflowRepository>();
        connectors.Setup(x => x.GetByIdAsync(connector.Id, It.IsAny<CancellationToken>())).ReturnsAsync(connector);
        workflows.Setup(x => x.HasActiveWorkflowForConnectorAsync(connector.Id, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        await new DeactivateConnectorHandler(connectors.Object, workflows.Object)
            .Handle(new DeactivateConnectorCommand(connector.Id), CancellationToken.None);

        connector.Status.Should().Be(ConnectorStatus.Inactive);
        connectors.Verify(x => x.UpdateAsync(connector, It.IsAny<CancellationToken>()), Times.Once);
    }

    private static Connector CreateActiveConnector() =>
        new("Orders API", "Orders connector", Guid.NewGuid(), "https://example.com",
            ConnectorProtocol.REST, ConnectorAuthenticationType.APIKey, 30, ConnectorStatus.Active);
}

