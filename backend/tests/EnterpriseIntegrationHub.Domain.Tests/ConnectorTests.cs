using System;
using EnterpriseIntegrationHub.Domain.Entities;
using EnterpriseIntegrationHub.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace EnterpriseIntegrationHub.Domain.Tests;

public class ConnectorTests
{
  [Fact]
  public void Constructor_WithValidParameters_CreatesConnectorWithCorrectProperties()
  {
    // Arrange
    var name = "Test Connector";
    var description = "Test Description";
    var externalSystemId = Guid.NewGuid();
    var baseUrl = "https://api.example.com";
    var protocol = ConnectorProtocol.REST;
    var authType = ConnectorAuthenticationType.APIKey;
    var timeoutSeconds = 30;
    var status = ConnectorStatus.Draft;

    // Act
    var connector = new Connector(name, description, externalSystemId, baseUrl, protocol, authType, timeoutSeconds, status);

    // Assert
    connector.Should().NotBeNull();
    connector.Id.Should().NotBe(Guid.Empty);
    connector.Name.Should().Be(name);
    connector.Description.Should().Be(description);
    connector.ExternalSystemId.Should().Be(externalSystemId);
    connector.BaseUrl.Should().Be(baseUrl);
    connector.Protocol.Should().Be(protocol);
    connector.AuthenticationType.Should().Be(authType);
    connector.TimeoutSeconds.Should().Be(timeoutSeconds);
    connector.Status.Should().Be(status);
    connector.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
    connector.UpdatedAt.Should().BeNull();
  }

  [Theory]
  [InlineData(ConnectorProtocol.REST)]
  [InlineData(ConnectorProtocol.SOAP)]
  [InlineData(ConnectorProtocol.SFTP)]
  [InlineData(ConnectorProtocol.GraphQL)]
  public void Constructor_WithDifferentProtocols_SetsProtocolCorrectly(ConnectorProtocol protocol)
  {
    // Act
    var connector = CreateConnector(protocol: protocol);

    // Assert
    connector.Protocol.Should().Be(protocol);
  }

  [Theory]
  [InlineData(ConnectorAuthenticationType.APIKey)]
  [InlineData(ConnectorAuthenticationType.OAuth2)]
  [InlineData(ConnectorAuthenticationType.Basic)]
  [InlineData(ConnectorAuthenticationType.BearerToken)]
  public void Constructor_WithDifferentAuthTypes_SetsAuthTypeCorrectly(ConnectorAuthenticationType authType)
  {
    // Act
    var connector = CreateConnector(authType: authType);

    // Assert
    connector.AuthenticationType.Should().Be(authType);
  }

  [Theory]
  [InlineData(ConnectorStatus.Draft)]
  [InlineData(ConnectorStatus.Active)]
  [InlineData(ConnectorStatus.Inactive)]
  public void Constructor_WithDifferentStatuses_SetsStatusCorrectly(ConnectorStatus status)
  {
    // Act
    var connector = CreateConnector(status: status);

    // Assert
    connector.Status.Should().Be(status);
  }

  [Fact]
  public void Constructor_WithZeroTimeout_AcceptsZero()
  {
    // Act
    var connector = CreateConnector(timeoutSeconds: 0);

    // Assert
    connector.TimeoutSeconds.Should().Be(0);
  }

  [Fact]
  public void Constructor_WithLargeTimeout_AcceptsLargeValue()
  {
    // Act
    var connector = CreateConnector(timeoutSeconds: 3600);

    // Assert
    connector.TimeoutSeconds.Should().Be(3600);
  }

  [Fact]
  public void Constructor_GeneratesUniqueIds()
  {
    // Act
    var connector1 = CreateConnector();
    var connector2 = CreateConnector();

    // Assert
    connector1.Id.Should().NotBe(connector2.Id);
  }

  [Fact]
  public void Constructor_SetsCreatedAtToCurrentTime()
  {
    // Arrange
    var before = DateTimeOffset.UtcNow;

    // Act
    var connector = CreateConnector();

    // Assert
    connector.CreatedAt.Should().BeOnOrAfter(before);
    connector.CreatedAt.Should().BeOnOrBefore(DateTimeOffset.UtcNow.AddSeconds(1));
  }

  [Fact]
  public void Constructor_SetsUpdatedAtToNull()
  {
    // Act
    var connector = CreateConnector();

    // Assert
    connector.UpdatedAt.Should().BeNull();
  }

  [Fact]
  public void Properties_HavePrivateSetters()
  {
    // Arrange
    var connector = CreateConnector();

    // Act & Assert - Properties should have private setters (not public)
    connector.GetType().GetProperty(nameof(Connector.Name))!.SetMethod!.IsPrivate.Should().BeTrue();
    connector.GetType().GetProperty(nameof(Connector.Description))!.SetMethod!.IsPrivate.Should().BeTrue();
    connector.GetType().GetProperty(nameof(Connector.ExternalSystemId))!.SetMethod!.IsPrivate.Should().BeTrue();
    connector.GetType().GetProperty(nameof(Connector.BaseUrl))!.SetMethod!.IsPrivate.Should().BeTrue();
    connector.GetType().GetProperty(nameof(Connector.Protocol))!.SetMethod!.IsPrivate.Should().BeTrue();
    connector.GetType().GetProperty(nameof(Connector.AuthenticationType))!.SetMethod!.IsPrivate.Should().BeTrue();
    connector.GetType().GetProperty(nameof(Connector.TimeoutSeconds))!.SetMethod!.IsPrivate.Should().BeTrue();
    connector.GetType().GetProperty(nameof(Connector.Status))!.SetMethod!.IsPrivate.Should().BeTrue();
  }

  private static Connector CreateConnector(
      string name = "Test Connector",
      string description = "Test Description",
      Guid? externalSystemId = null,
      string baseUrl = "https://api.example.com",
      ConnectorProtocol protocol = ConnectorProtocol.REST,
      ConnectorAuthenticationType authType = ConnectorAuthenticationType.APIKey,
      int timeoutSeconds = 30,
      ConnectorStatus status = ConnectorStatus.Draft)
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