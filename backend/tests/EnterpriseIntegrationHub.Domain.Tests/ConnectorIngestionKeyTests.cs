using System;
using EnterpriseIntegrationHub.Domain.Entities;
using EnterpriseIntegrationHub.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace EnterpriseIntegrationHub.Domain.Tests;

public class ConnectorIngestionKeyTests
{
    private static Connector CreateConnector() =>
        new("Test Connector", "Description", Guid.NewGuid(), "https://api.example.com",
            ConnectorProtocol.REST, ConnectorAuthenticationType.APIKey, 30, ConnectorStatus.Active);

    [Fact]
    public void AssignIngestionKeyHash_WithValidHash_StoresLowercaseHashAndUpdatesTimestamp()
    {
        var connector = CreateConnector();
        var hash = new string('A', 64);

        connector.AssignIngestionKeyHash(hash);

        connector.IngestionKeyHash.Should().Be(new string('a', 64));
        connector.UpdatedAt.Should().NotBeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abc")]
    [InlineData("zzzz")]
    public void AssignIngestionKeyHash_WithInvalidHash_ThrowsArgumentException(string hash)
    {
        var connector = CreateConnector();

        var act = () => connector.AssignIngestionKeyHash(hash);

        act.Should().Throw<ArgumentException>();
        connector.IngestionKeyHash.Should().BeNull();
    }
}
