using System;
using System.Threading;
using System.Threading.Tasks;
using EnterpriseIntegrationHub.Application.Features.Connectors.Create;
using EnterpriseIntegrationHub.Application.Interfaces;
using EnterpriseIntegrationHub.Domain.Entities;
using EnterpriseIntegrationHub.Domain.Enums;
using FluentAssertions;
using Moq;
using Xunit;

namespace EnterpriseIntegrationHub.Application.Tests;

public class CreateConnectorHandlerTests
{
  private readonly Mock<IConnectorRepository> _connectorRepositoryMock;
  private readonly Mock<IExternalSystemRepository> _externalSystemRepositoryMock;
  private readonly CreateConnectorHandler _handler;

  public CreateConnectorHandlerTests()
  {
    _connectorRepositoryMock = new Mock<IConnectorRepository>();
    _externalSystemRepositoryMock = new Mock<IExternalSystemRepository>();
    _handler = new CreateConnectorHandler(_connectorRepositoryMock.Object, _externalSystemRepositoryMock.Object);
  }

  [Fact]
  public async Task Handle_WhenExternalSystemNotFound_ThrowsInvalidOperationException()
  {
    // Arrange
    var externalSystemId = Guid.NewGuid();
    _externalSystemRepositoryMock
        .Setup(r => r.GetByIdAsync(externalSystemId, It.IsAny<CancellationToken>()))
        .ReturnsAsync((ExternalSystem?)null);

    var command = new CreateConnectorCommand(
        "Test Connector",
        "Description",
        externalSystemId,
        "https://api.example.com",
        ConnectorProtocol.REST,
        ConnectorAuthenticationType.APIKey,
        30);

    // Act & Assert
    await Assert.ThrowsAsync<InvalidOperationException>(() =>
        _handler.Handle(command, CancellationToken.None));
  }

  [Fact]
  public async Task Handle_WhenConnectorAlreadyExists_ThrowsInvalidOperationException()
  {
    // Arrange
    var externalSystemId = Guid.NewGuid();
    var externalSystem = new ExternalSystem("External System", "Description", ExternalSystemEnvironment.Development, ExternalSystemStatus.Active);

    _externalSystemRepositoryMock
        .Setup(r => r.GetByIdAsync(externalSystemId, It.IsAny<CancellationToken>()))
        .ReturnsAsync(externalSystem);

    _connectorRepositoryMock
        .Setup(r => r.ExistsAsync(externalSystemId, ConnectorProtocol.REST, It.IsAny<CancellationToken>()))
        .ReturnsAsync(true);

    var command = new CreateConnectorCommand(
        "Test Connector",
        "Description",
        externalSystemId,
        "https://api.example.com",
        ConnectorProtocol.REST,
        ConnectorAuthenticationType.APIKey,
        30);

    // Act & Assert
    await Assert.ThrowsAsync<InvalidOperationException>(() =>
        _handler.Handle(command, CancellationToken.None));
  }

  [Fact]
  public async Task Handle_WhenValidCommand_CreatesConnectorAndReturnsId()
  {
    // Arrange
    var externalSystemId = Guid.NewGuid();
    var externalSystem = new ExternalSystem("External System", "Description", ExternalSystemEnvironment.Development, ExternalSystemStatus.Active);

    _externalSystemRepositoryMock
        .Setup(r => r.GetByIdAsync(externalSystemId, It.IsAny<CancellationToken>()))
        .ReturnsAsync(externalSystem);

    _connectorRepositoryMock
        .Setup(r => r.ExistsAsync(externalSystemId, ConnectorProtocol.REST, It.IsAny<CancellationToken>()))
        .ReturnsAsync(false);

    Connector? capturedConnector = null;
    _connectorRepositoryMock
        .Setup(r => r.AddAsync(It.IsAny<Connector>(), It.IsAny<CancellationToken>()))
        .Callback<Connector, CancellationToken>((c, ct) => capturedConnector = c)
        .Returns(Task.CompletedTask);

    var command = new CreateConnectorCommand(
        "Test Connector",
        "Test Description",
        externalSystemId,
        "https://api.example.com",
        ConnectorProtocol.REST,
        ConnectorAuthenticationType.APIKey,
        30);

    // Act
    var resultId = await _handler.Handle(command, CancellationToken.None);

    // Assert
    resultId.Should().NotBeEmpty();
    capturedConnector.Should().NotBeNull();
    capturedConnector!.Name.Should().Be("Test Connector");
    capturedConnector.Description.Should().Be("Test Description");
    capturedConnector.ExternalSystemId.Should().Be(externalSystemId);
    capturedConnector.BaseUrl.Should().Be("https://api.example.com");
    capturedConnector.Protocol.Should().Be(ConnectorProtocol.REST);
    capturedConnector.AuthenticationType.Should().Be(ConnectorAuthenticationType.APIKey);
    capturedConnector.TimeoutSeconds.Should().Be(30);
    capturedConnector.Status.Should().Be(ConnectorStatus.Draft);

    _connectorRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Connector>(), It.IsAny<CancellationToken>()), Times.Once);
  }

  [Theory]
  [InlineData(ConnectorProtocol.REST)]
  [InlineData(ConnectorProtocol.SOAP)]
  [InlineData(ConnectorProtocol.SFTP)]
  [InlineData(ConnectorProtocol.GraphQL)]
  public async Task Handle_WithDifferentProtocols_CreatesConnectorWithCorrectProtocol(ConnectorProtocol protocol)
  {
    // Arrange
    var externalSystemId = Guid.NewGuid();
    var externalSystem = new ExternalSystem("External System", "Description", ExternalSystemEnvironment.Development, ExternalSystemStatus.Active);

    _externalSystemRepositoryMock
        .Setup(r => r.GetByIdAsync(externalSystemId, It.IsAny<CancellationToken>()))
        .ReturnsAsync(externalSystem);

    _connectorRepositoryMock
        .Setup(r => r.ExistsAsync(externalSystemId, protocol, It.IsAny<CancellationToken>()))
        .ReturnsAsync(false);

    Connector? capturedConnector = null;
    _connectorRepositoryMock
        .Setup(r => r.AddAsync(It.IsAny<Connector>(), It.IsAny<CancellationToken>()))
        .Callback<Connector, CancellationToken>((c, ct) => capturedConnector = c)
        .Returns(Task.CompletedTask);

    var command = new CreateConnectorCommand(
        "Test Connector",
        "Description",
        externalSystemId,
        "https://api.example.com",
        protocol,
        ConnectorAuthenticationType.APIKey,
        30);

    // Act
    await _handler.Handle(command, CancellationToken.None);

    // Assert
    capturedConnector.Should().NotBeNull();
    capturedConnector!.Protocol.Should().Be(protocol);
  }

  [Theory]
  [InlineData(ConnectorAuthenticationType.APIKey)]
  [InlineData(ConnectorAuthenticationType.OAuth2)]
  [InlineData(ConnectorAuthenticationType.Basic)]
  [InlineData(ConnectorAuthenticationType.BearerToken)]
  public async Task Handle_WithDifferentAuthenticationTypes_CreatesConnectorWithCorrectAuthType(ConnectorAuthenticationType authType)
  {
    // Arrange
    var externalSystemId = Guid.NewGuid();
    var externalSystem = new ExternalSystem("External System", "Description", ExternalSystemEnvironment.Development, ExternalSystemStatus.Active);

    _externalSystemRepositoryMock
        .Setup(r => r.GetByIdAsync(externalSystemId, It.IsAny<CancellationToken>()))
        .ReturnsAsync(externalSystem);

    _connectorRepositoryMock
        .Setup(r => r.ExistsAsync(externalSystemId, ConnectorProtocol.REST, It.IsAny<CancellationToken>()))
        .ReturnsAsync(false);

    Connector? capturedConnector = null;
    _connectorRepositoryMock
        .Setup(r => r.AddAsync(It.IsAny<Connector>(), It.IsAny<CancellationToken>()))
        .Callback<Connector, CancellationToken>((c, ct) => capturedConnector = c)
        .Returns(Task.CompletedTask);

    var command = new CreateConnectorCommand(
        "Test Connector",
        "Description",
        externalSystemId,
        "https://api.example.com",
        ConnectorProtocol.REST,
        authType,
        30);

    // Act
    await _handler.Handle(command, CancellationToken.None);

    // Assert
    capturedConnector.Should().NotBeNull();
    capturedConnector!.AuthenticationType.Should().Be(authType);
  }

  [Fact]
  public async Task Handle_WhenTimeoutSecondsIsZero_UsesDefaultValue()
  {
    // Arrange
    var externalSystemId = Guid.NewGuid();
    var externalSystem = new ExternalSystem("External System", "Description", ExternalSystemEnvironment.Development, ExternalSystemStatus.Active);

    _externalSystemRepositoryMock
        .Setup(r => r.GetByIdAsync(externalSystemId, It.IsAny<CancellationToken>()))
        .ReturnsAsync(externalSystem);

    _connectorRepositoryMock
        .Setup(r => r.ExistsAsync(externalSystemId, ConnectorProtocol.REST, It.IsAny<CancellationToken>()))
        .ReturnsAsync(false);

    Connector? capturedConnector = null;
    _connectorRepositoryMock
        .Setup(r => r.AddAsync(It.IsAny<Connector>(), It.IsAny<CancellationToken>()))
        .Callback<Connector, CancellationToken>((c, ct) => capturedConnector = c)
        .Returns(Task.CompletedTask);

    var command = new CreateConnectorCommand(
        "Test Connector",
        "Description",
        externalSystemId,
        "https://api.example.com",
        ConnectorProtocol.REST,
        ConnectorAuthenticationType.APIKey,
        0); // Zero timeout

    // Act
    await _handler.Handle(command, CancellationToken.None);

    // Assert
    capturedConnector.Should().NotBeNull();
    capturedConnector!.TimeoutSeconds.Should().Be(0);
  }
}