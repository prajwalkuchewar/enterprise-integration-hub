using EnterpriseIntegrationHub.Application.Features.Connectors.Update;
using EnterpriseIntegrationHub.Application.Interfaces;
using EnterpriseIntegrationHub.Domain.Entities;
using EnterpriseIntegrationHub.Domain.Enums;
using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace EnterpriseIntegrationHub.Application.Tests;

public class UpdateConnectorHandlerTests
{
  private readonly Mock<IConnectorRepository> _connectorRepositoryMock;
  private readonly Mock<IExternalSystemRepository> _externalSystemRepositoryMock;
  private readonly UpdateConnectorHandler _handler;

  public UpdateConnectorHandlerTests()
  {
    _connectorRepositoryMock = new Mock<IConnectorRepository>();
    _externalSystemRepositoryMock = new Mock<IExternalSystemRepository>();
    _handler = new UpdateConnectorHandler(_connectorRepositoryMock.Object, _externalSystemRepositoryMock.Object);
  }

  [Fact]
  public async Task Handle_WhenConnectorNotFound_ThrowsKeyNotFoundException()
  {
    var id = Guid.NewGuid();
    _connectorRepositoryMock
      .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
      .ReturnsAsync((Connector?)null);

    var command = new UpdateConnectorCommand("Name", "Desc", "https://api.example.com", ConnectorProtocol.REST, ConnectorAuthenticationType.APIKey, 30);

    await Assert.ThrowsAsync<KeyNotFoundException>(() => _handler.Handle(id, command, CancellationToken.None));
  }

  [Fact]
  public async Task Handle_WhenExternalSystemNotFound_ThrowsKeyNotFoundException()
  {
    var id = Guid.NewGuid();
    var connector = new Connector("Old", "D", Guid.NewGuid(), "https://old", ConnectorProtocol.REST, ConnectorAuthenticationType.APIKey, 10, ConnectorStatus.Active);

    _connectorRepositoryMock
      .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
      .ReturnsAsync(connector);

    _externalSystemRepositoryMock
      .Setup(r => r.GetByIdAsync(connector.ExternalSystemId, It.IsAny<CancellationToken>()))
      .ReturnsAsync((ExternalSystem?)null);

    var command = new UpdateConnectorCommand("Name", "Desc", "https://api.example.com", ConnectorProtocol.REST, ConnectorAuthenticationType.APIKey, 30);

    await Assert.ThrowsAsync<KeyNotFoundException>(() => _handler.Handle(id, command, CancellationToken.None));
  }

  [Fact]
  public async Task Handle_WhenExternalSystemInactive_ThrowsInvalidOperationException()
  {
    var id = Guid.NewGuid();
    var external = new ExternalSystem("ES", "D", ExternalSystemEnvironment.Development, ExternalSystemStatus.Inactive);
    var connector = new Connector("Old", "D", external.Id, "https://old", ConnectorProtocol.REST, ConnectorAuthenticationType.APIKey, 10, ConnectorStatus.Active);

    _connectorRepositoryMock
      .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
      .ReturnsAsync(connector);

    _externalSystemRepositoryMock
      .Setup(r => r.GetByIdAsync(connector.ExternalSystemId, It.IsAny<CancellationToken>()))
      .ReturnsAsync(external);

    var command = new UpdateConnectorCommand("Name", "Desc", "https://api.example.com", ConnectorProtocol.REST, ConnectorAuthenticationType.APIKey, 30);

    await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.Handle(id, command, CancellationToken.None));
  }

  [Fact]
  public async Task Handle_WhenNameConflict_ThrowsInvalidOperationException()
  {
    var id = Guid.NewGuid();
    var external = new ExternalSystem("ES", "D", ExternalSystemEnvironment.Development, ExternalSystemStatus.Active);
    var connector = new Connector("Old", "D", external.Id, "https://old", ConnectorProtocol.REST, ConnectorAuthenticationType.APIKey, 10, ConnectorStatus.Active);

    var other = new Connector("Conflicting", "D2", external.Id, "https://x", ConnectorProtocol.REST, ConnectorAuthenticationType.APIKey, 10, ConnectorStatus.Draft);

    _connectorRepositoryMock
      .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
      .ReturnsAsync(connector);

    _externalSystemRepositoryMock
      .Setup(r => r.GetByIdAsync(connector.ExternalSystemId, It.IsAny<CancellationToken>()))
      .ReturnsAsync(external);

    _connectorRepositoryMock
      .Setup(r => r.ExistsAsync(connector.ExternalSystemId, "Conflicting", It.IsAny<CancellationToken>(), connector.Id))
      .ReturnsAsync(true);

    var command = new UpdateConnectorCommand("Conflicting", "Desc", "https://api.example.com", ConnectorProtocol.REST, ConnectorAuthenticationType.APIKey, 30);

    await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.Handle(id, command, CancellationToken.None));
  }

  [Fact]
  public async Task Handle_WhenOnlyBasicInfoChanged_KeepsStatusAndUpdates()
  {
    var id = Guid.NewGuid();
    var external = new ExternalSystem("ES", "D", ExternalSystemEnvironment.Development, ExternalSystemStatus.Active);
    var connector = new Connector("OldName", "OldDesc", external.Id, "https://old", ConnectorProtocol.REST, ConnectorAuthenticationType.APIKey, 10, ConnectorStatus.Active);

    Connector? updated = null;

    _connectorRepositoryMock
      .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
      .ReturnsAsync(connector);

    _externalSystemRepositoryMock
      .Setup(r => r.GetByIdAsync(connector.ExternalSystemId, It.IsAny<CancellationToken>()))
      .ReturnsAsync(external);

    _connectorRepositoryMock
      .Setup(r => r.ExistsAsync(connector.ExternalSystemId, "NewName", It.IsAny<CancellationToken>(), connector.Id))
      .ReturnsAsync(false);

    _connectorRepositoryMock
      .Setup(r => r.UpdateAsync(It.IsAny<Connector>(), It.IsAny<CancellationToken>()))
      .Callback<Connector, CancellationToken>((c, ct) => updated = c)
      .Returns(Task.CompletedTask);

    var command = new UpdateConnectorCommand("NewName", "NewDesc", "https://old", ConnectorProtocol.REST, ConnectorAuthenticationType.APIKey, 10);

    await _handler.Handle(id, command, CancellationToken.None);

    updated.Should().NotBeNull();
    updated!.Name.Should().Be("NewName");
    updated.Description.Should().Be("NewDesc");
    updated.Status.Should().Be(ConnectorStatus.Active);
    updated.UpdatedAt.Should().NotBeNull();
  }

  [Fact]
  public async Task Handle_WhenCommunicationChanged_ActiveBecomesDraft()
  {
    var id = Guid.NewGuid();
    var external = new ExternalSystem("ES", "D", ExternalSystemEnvironment.Development, ExternalSystemStatus.Active);
    var connector = new Connector("OldName", "OldDesc", external.Id, "https://old", ConnectorProtocol.REST, ConnectorAuthenticationType.APIKey, 10, ConnectorStatus.Active);

    Connector? updated = null;

    _connectorRepositoryMock
      .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
      .ReturnsAsync(connector);

    _externalSystemRepositoryMock
      .Setup(r => r.GetByIdAsync(connector.ExternalSystemId, It.IsAny<CancellationToken>()))
      .ReturnsAsync(external);

    _connectorRepositoryMock
      .Setup(r => r.ExistsAsync(connector.ExternalSystemId, "OldName", It.IsAny<CancellationToken>(), connector.Id))
      .ReturnsAsync(false);

    _connectorRepositoryMock
      .Setup(r => r.UpdateAsync(It.IsAny<Connector>(), It.IsAny<CancellationToken>()))
      .Callback<Connector, CancellationToken>((c, ct) => updated = c)
      .Returns(Task.CompletedTask);

    var command = new UpdateConnectorCommand("OldName", "OldDesc", "https://new", ConnectorProtocol.REST, ConnectorAuthenticationType.APIKey, 20);

    await _handler.Handle(id, command, CancellationToken.None);

    updated.Should().NotBeNull();
    updated!.BaseUrl.Should().Be("https://new");
    updated.TimeoutSeconds.Should().Be(20);
    updated.Status.Should().Be(ConnectorStatus.Draft);
    updated.UpdatedAt.Should().NotBeNull();
  }

  [Fact]
  public async Task Handle_WhenInvalidBaseUrl_ThrowsArgumentException()
  {
    var id = Guid.NewGuid();
    var external = new ExternalSystem("ES", "D", ExternalSystemEnvironment.Development, ExternalSystemStatus.Active);
    var connector = new Connector("Old", "D", external.Id, "https://old", ConnectorProtocol.REST, ConnectorAuthenticationType.APIKey, 10, ConnectorStatus.Draft);

    _connectorRepositoryMock
      .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
      .ReturnsAsync(connector);

    _externalSystemRepositoryMock
      .Setup(r => r.GetByIdAsync(connector.ExternalSystemId, It.IsAny<CancellationToken>()))
      .ReturnsAsync(external);

    var command = new UpdateConnectorCommand("Name", "Desc", "not-a-url", ConnectorProtocol.REST, ConnectorAuthenticationType.APIKey, 10);

    await Assert.ThrowsAsync<ArgumentException>(() => _handler.Handle(id, command, CancellationToken.None));
  }

  [Fact]
  public async Task Handle_WhenTimeoutInvalid_ThrowsArgumentException()
  {
    var id = Guid.NewGuid();
    var external = new ExternalSystem("ES", "D", ExternalSystemEnvironment.Development, ExternalSystemStatus.Active);
    var connector = new Connector("Old", "D", external.Id, "https://old", ConnectorProtocol.REST, ConnectorAuthenticationType.APIKey, 10, ConnectorStatus.Draft);

    _connectorRepositoryMock
      .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
      .ReturnsAsync(connector);

    _externalSystemRepositoryMock
      .Setup(r => r.GetByIdAsync(connector.ExternalSystemId, It.IsAny<CancellationToken>()))
      .ReturnsAsync(external);

    var command = new UpdateConnectorCommand("Name", "Desc", "https://api.example.com", ConnectorProtocol.REST, ConnectorAuthenticationType.APIKey, 0);

    await Assert.ThrowsAsync<ArgumentException>(() => _handler.Handle(id, command, CancellationToken.None));
  }
}
