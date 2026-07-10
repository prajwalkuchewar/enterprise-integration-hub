using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnterpriseIntegrationHub.Application.Features.Connectors.ViewDetails;
using EnterpriseIntegrationHub.Application.Interfaces;
using EnterpriseIntegrationHub.Domain.Entities;
using EnterpriseIntegrationHub.Domain.Enums;
using FluentAssertions;
using Moq;
using Xunit;

namespace EnterpriseIntegrationHub.Application.Tests;

public class ViewDetailsConnectorHandlerTests
{
  [Fact]
  public async Task Handle_WhenConnectorNotFound_ThrowsKeyNotFoundException()
  {
    // Arrange
    var repo = new Mock<IConnectorRepository>();
    repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync((Connector?)null);

    var handler = new ViewConnectorDetailsHandler(repo.Object);
    var query = new ViewConnectorDetailsQuery(Guid.NewGuid());

    // Act & Assert
    await Assert.ThrowsAsync<KeyNotFoundException>(() =>
        handler.Handle(query, CancellationToken.None));
  }

  [Fact]
  public async Task Handle_WhenConnectorFound_ReturnsMappedSummary()
  {
    // Arrange
    var externalSystemId = Guid.NewGuid();
    var connector = new Connector(
        "Test Connector",
        "Test Description",
        externalSystemId,
        "https://api.example.com",
        ConnectorProtocol.REST,
        ConnectorAuthenticationType.APIKey,
        30,
        ConnectorStatus.Active);

    var repo = new Mock<IConnectorRepository>();
    repo.Setup(r => r.GetByIdAsync(connector.Id, It.IsAny<CancellationToken>()))
        .ReturnsAsync(connector);

    var handler = new ViewConnectorDetailsHandler(repo.Object);
    var query = new ViewConnectorDetailsQuery(connector.Id);

    // Act
    var result = await handler.Handle(query, CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result.Id.Should().Be(connector.Id);
    result.Name.Should().Be(connector.Name);
    result.Description.Should().Be(connector.Description);
    result.ExternalSystemId.Should().Be(connector.ExternalSystemId);
    result.BaseUrl.Should().Be(connector.BaseUrl);
    result.Protocol.Should().Be(connector.Protocol);
    result.AuthenticationType.Should().Be(connector.AuthenticationType);
    result.TimeoutSeconds.Should().Be(connector.TimeoutSeconds);
    result.Status.Should().Be(connector.Status);
  }

  [Theory]
  [InlineData(ConnectorProtocol.REST)]
  [InlineData(ConnectorProtocol.SOAP)]
  [InlineData(ConnectorProtocol.SFTP)]
  [InlineData(ConnectorProtocol.GraphQL)]
  public async Task Handle_WithDifferentProtocols_ReturnsCorrectProtocol(ConnectorProtocol protocol)
  {
    // Arrange
    var connector = new Connector(
        "Test Connector",
        "Description",
        Guid.NewGuid(),
        "https://api.example.com",
        protocol,
        ConnectorAuthenticationType.APIKey,
        30,
        ConnectorStatus.Active);

    var repo = new Mock<IConnectorRepository>();
    repo.Setup(r => r.GetByIdAsync(connector.Id, It.IsAny<CancellationToken>()))
        .ReturnsAsync(connector);

    var handler = new ViewConnectorDetailsHandler(repo.Object);
    var query = new ViewConnectorDetailsQuery(connector.Id);

    // Act
    var result = await handler.Handle(query, CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result.Protocol.Should().Be(protocol);
  }

  [Theory]
  [InlineData(ConnectorAuthenticationType.APIKey)]
  [InlineData(ConnectorAuthenticationType.OAuth2)]
  [InlineData(ConnectorAuthenticationType.Basic)]
  [InlineData(ConnectorAuthenticationType.BearerToken)]
  public async Task Handle_WithDifferentAuthTypes_ReturnsCorrectAuthType(ConnectorAuthenticationType authType)
  {
    // Arrange
    var connector = new Connector(
        "Test Connector",
        "Description",
        Guid.NewGuid(),
        "https://api.example.com",
        ConnectorProtocol.REST,
        authType,
        30,
        ConnectorStatus.Active);

    var repo = new Mock<IConnectorRepository>();
    repo.Setup(r => r.GetByIdAsync(connector.Id, It.IsAny<CancellationToken>()))
        .ReturnsAsync(connector);

    var handler = new ViewConnectorDetailsHandler(repo.Object);
    var query = new ViewConnectorDetailsQuery(connector.Id);

    // Act
    var result = await handler.Handle(query, CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result.AuthenticationType.Should().Be(authType);
  }

  [Theory]
  [InlineData(ConnectorStatus.Draft)]
  [InlineData(ConnectorStatus.Active)]
  [InlineData(ConnectorStatus.Inactive)]
  public async Task Handle_WithDifferentStatuses_ReturnsCorrectStatus(ConnectorStatus status)
  {
    // Arrange
    var connector = new Connector(
        "Test Connector",
        "Description",
        Guid.NewGuid(),
        "https://api.example.com",
        ConnectorProtocol.REST,
        ConnectorAuthenticationType.APIKey,
        30,
        status);

    var repo = new Mock<IConnectorRepository>();
    repo.Setup(r => r.GetByIdAsync(connector.Id, It.IsAny<CancellationToken>()))
        .ReturnsAsync(connector);

    var handler = new ViewConnectorDetailsHandler(repo.Object);
    var query = new ViewConnectorDetailsQuery(connector.Id);

    // Act
    var result = await handler.Handle(query, CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result.Status.Should().Be(status);
  }
}