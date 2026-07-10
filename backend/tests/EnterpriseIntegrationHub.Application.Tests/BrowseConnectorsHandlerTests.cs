using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnterpriseIntegrationHub.Application.Features.Connectors.Browse;
using EnterpriseIntegrationHub.Application.Interfaces;
using EnterpriseIntegrationHub.Domain.Entities;
using EnterpriseIntegrationHub.Domain.Enums;
using FluentAssertions;
using Moq;
using Xunit;

namespace EnterpriseIntegrationHub.Application.Tests;

public class BrowseConnectorsHandlerTests
{
  [Fact]
  public async Task Handle_ReturnsMappedSummariesAndTotalCount()
  {
    // Arrange
    var items = new List<Connector>
        {
            CreateConnector("Connector A", "Description A", ConnectorProtocol.REST, ConnectorAuthenticationType.APIKey, ConnectorStatus.Active),
            CreateConnector("Connector B", "Description B", ConnectorProtocol.SOAP, ConnectorAuthenticationType.OAuth2, ConnectorStatus.Draft),
            CreateConnector("Connector C", "Description C", ConnectorProtocol.GraphQL, ConnectorAuthenticationType.BearerToken, ConnectorStatus.Inactive)
        };

    var repo = new Mock<IConnectorRepository>();
    repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
        .ReturnsAsync(items.AsReadOnly());

    var handler = new BrowseConnectorsHandler(repo.Object);
    var query = new BrowseConnectorsQuery();

    // Act
    var result = await handler.Handle(query, CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result.TotalCount.Should().Be(items.Count);
    result.Items.Should().HaveCount(items.Count);

    var resultNames = result.Items.Select(i => i.Name).ToList();
    var expectedNames = items.Select(x => x.Name).OrderBy(x => x).ToList();
    resultNames.Should().BeEquivalentTo(expectedNames);
  }

  [Fact]
  public async Task Handle_WhenNoConnectors_ReturnsEmptyListAndZeroCount()
  {
    // Arrange
    var repo = new Mock<IConnectorRepository>();
    repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
        .ReturnsAsync(new List<Connector>().AsReadOnly());

    var handler = new BrowseConnectorsHandler(repo.Object);
    var query = new BrowseConnectorsQuery();

    // Act
    var result = await handler.Handle(query, CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result.TotalCount.Should().Be(0);
    result.Items.Should().BeEmpty();
  }

  [Fact]
  public async Task Handle_MapsAllConnectorPropertiesCorrectly()
  {
    // Arrange
    var externalSystemId = Guid.NewGuid();
    var connector = CreateConnector(
        "Test Connector",
        "Test Description",
        ConnectorProtocol.REST,
        ConnectorAuthenticationType.APIKey,
        ConnectorStatus.Active,
        externalSystemId,
        "https://api.example.com",
        60);

    var repo = new Mock<IConnectorRepository>();
    repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
        .ReturnsAsync(new List<Connector> { connector }.AsReadOnly());

    var handler = new BrowseConnectorsHandler(repo.Object);
    var query = new BrowseConnectorsQuery();

    // Act
    var result = await handler.Handle(query, CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result.Items.Should().HaveCount(1);

    var summary = result.Items.First();
    summary.Id.Should().Be(connector.Id);
    summary.Name.Should().Be(connector.Name);
    summary.Description.Should().Be(connector.Description);
    summary.ExternalSystemId.Should().Be(connector.ExternalSystemId);
    summary.BaseUrl.Should().Be(connector.BaseUrl);
    summary.Protocol.Should().Be(connector.Protocol);
    summary.AuthenticationType.Should().Be(connector.AuthenticationType);
    summary.TimeoutSeconds.Should().Be(connector.TimeoutSeconds);
    summary.Status.Should().Be(connector.Status);
  }

  [Fact]
  public async Task Handle_ReturnsConnectorsOrderedByName()
  {
    // Arrange
    var items = new List<Connector>
        {
            CreateConnector("Zebra Connector", "Desc", ConnectorProtocol.REST, ConnectorAuthenticationType.APIKey, ConnectorStatus.Active),
            CreateConnector("Alpha Connector", "Desc", ConnectorProtocol.REST, ConnectorAuthenticationType.APIKey, ConnectorStatus.Active),
            CreateConnector("Beta Connector", "Desc", ConnectorProtocol.REST, ConnectorAuthenticationType.APIKey, ConnectorStatus.Active)
        };

    var repo = new Mock<IConnectorRepository>();
    repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
        .ReturnsAsync(items.AsReadOnly());

    var handler = new BrowseConnectorsHandler(repo.Object);
    var query = new BrowseConnectorsQuery();

    // Act
    var result = await handler.Handle(query, CancellationToken.None);

    // Assert
    var names = result.Items.Select(i => i.Name).ToList();
    names.Should().BeInAscendingOrder();
  }

  private static Connector CreateConnector(
      string name,
      string description,
      ConnectorProtocol protocol,
      ConnectorAuthenticationType authType,
      ConnectorStatus status,
      Guid? externalSystemId = null,
      string baseUrl = "https://api.example.com",
      int timeoutSeconds = 30)
  {
    return new Connector(
        name,
        description,
        externalSystemId ?? Guid.NewGuid(),
        baseUrl,
        protocol,
        authType,
        timeoutSeconds,
        status);
  }
}