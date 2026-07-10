using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnterpriseIntegrationHub.Application.Features.Connectors.Activate;
using EnterpriseIntegrationHub.Application.Interfaces;
using EnterpriseIntegrationHub.Domain.Entities;
using EnterpriseIntegrationHub.Domain.Enums;
using FluentAssertions;
using Moq;
using Xunit;

namespace EnterpriseIntegrationHub.Application.Tests;

public class ActivateConnectorHandlerTests
{
  private readonly Mock<IConnectorRepository> _connectorRepositoryMock;
  private readonly Mock<IExternalSystemRepository> _externalSystemRepositoryMock;
  private readonly ActivateConnectorHandler _handler;

  public ActivateConnectorHandlerTests()
  {
    _connectorRepositoryMock = new Mock<IConnectorRepository>();
    _externalSystemRepositoryMock = new Mock<IExternalSystemRepository>();
    _handler = new ActivateConnectorHandler(_connectorRepositoryMock.Object, _externalSystemRepositoryMock.Object);
  }

  [Fact]
  public async Task Handle_WhenConnectorNotFound_ThrowsKeyNotFoundException()
  {
    var connectorId = Guid.NewGuid();

    _connectorRepositoryMock
        .Setup(r => r.GetByIdAsync(connectorId, It.IsAny<CancellationToken>()))
        .ReturnsAsync((Connector?)null);

    var command = new ActivateConnectorCommand(connectorId);

    await Assert.ThrowsAsync<KeyNotFoundException>(() => _handler.Handle(command, CancellationToken.None));
  }

  [Fact]
  public async Task Handle_WhenConnectorNotDraft_ThrowsInvalidOperationException()
  {
    var connector = new Connector(
        "Test Connector",
        "Description",
        Guid.NewGuid(),
        "https://api.example.com",
        ConnectorProtocol.REST,
        ConnectorAuthenticationType.APIKey,
        30,
        ConnectorStatus.Active);

    _connectorRepositoryMock
        .Setup(r => r.GetByIdAsync(connector.Id, It.IsAny<CancellationToken>()))
        .ReturnsAsync(connector);

    var command = new ActivateConnectorCommand(connector.Id);

    await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.Handle(command, CancellationToken.None));
  }

  [Fact]
  public async Task Handle_WhenExternalSystemNotFound_ThrowsKeyNotFoundException()
  {
    var externalSystemId = Guid.NewGuid();
    var connector = new Connector(
        "Test Connector",
        "Description",
        externalSystemId,
        "https://api.example.com",
        ConnectorProtocol.REST,
        ConnectorAuthenticationType.APIKey,
        30,
        ConnectorStatus.Draft);

    _connectorRepositoryMock
        .Setup(r => r.GetByIdAsync(connector.Id, It.IsAny<CancellationToken>()))
        .ReturnsAsync(connector);

    _externalSystemRepositoryMock
        .Setup(r => r.GetByIdAsync(externalSystemId, It.IsAny<CancellationToken>()))
        .ReturnsAsync((ExternalSystem?)null);

    var command = new ActivateConnectorCommand(connector.Id);

    await Assert.ThrowsAsync<KeyNotFoundException>(() => _handler.Handle(command, CancellationToken.None));
  }

  [Fact]
  public async Task Handle_WhenExternalSystemInactive_ThrowsInvalidOperationException()
  {
    var externalSystemId = Guid.NewGuid();
    var connector = new Connector(
        "Test Connector",
        "Description",
        externalSystemId,
        "https://api.example.com",
        ConnectorProtocol.REST,
        ConnectorAuthenticationType.APIKey,
        30,
        ConnectorStatus.Draft);

    _connectorRepositoryMock
        .Setup(r => r.GetByIdAsync(connector.Id, It.IsAny<CancellationToken>()))
        .ReturnsAsync(connector);

    var externalSystem = new ExternalSystem("External System", "Description", ExternalSystemEnvironment.Development, ExternalSystemStatus.Inactive);

    _externalSystemRepositoryMock
        .Setup(r => r.GetByIdAsync(externalSystemId, It.IsAny<CancellationToken>()))
        .ReturnsAsync(externalSystem);

    var command = new ActivateConnectorCommand(connector.Id);

    await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.Handle(command, CancellationToken.None));
  }

  [Fact]
  public async Task Handle_WhenValidCommand_ActivatesConnectorAndUpdatesIt()
  {
    var externalSystemId = Guid.NewGuid();
    var connector = new Connector(
        "Test Connector",
        "Description",
        externalSystemId,
        "https://api.example.com",
        ConnectorProtocol.REST,
        ConnectorAuthenticationType.APIKey,
        30,
        ConnectorStatus.Draft);

    _connectorRepositoryMock
        .Setup(r => r.GetByIdAsync(connector.Id, It.IsAny<CancellationToken>()))
        .ReturnsAsync(connector);

    var externalSystem = new ExternalSystem("External System", "Description", ExternalSystemEnvironment.Development, ExternalSystemStatus.Active);

    _externalSystemRepositoryMock
        .Setup(r => r.GetByIdAsync(externalSystemId, It.IsAny<CancellationToken>()))
        .ReturnsAsync(externalSystem);

    _connectorRepositoryMock
        .Setup(r => r.UpdateAsync(connector, It.IsAny<CancellationToken>()))
        .Returns(Task.CompletedTask)
        .Verifiable();

    var command = new ActivateConnectorCommand(connector.Id);

    await _handler.Handle(command, CancellationToken.None);

    connector.Status.Should().Be(ConnectorStatus.Active);
    connector.UpdatedAt.Should().NotBeNull();
    connector.UpdatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));

    _connectorRepositoryMock.Verify(r => r.UpdateAsync(connector, It.IsAny<CancellationToken>()), Times.Once);
  }
}
