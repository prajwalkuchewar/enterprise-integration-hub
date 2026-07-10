using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnterpriseIntegrationHub.Application.Interfaces;
using EnterpriseIntegrationHub.Domain.Entities;
using EnterpriseIntegrationHub.Domain.Enums;
using EnterpriseIntegrationHub.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EnterpriseIntegrationHub.Infrastructure.Tests;

public class ConnectorRepositoryTests : IDisposable
{
  private readonly EnterpriseIntegrationHubDbContext _context;
  private readonly IConnectorRepository _repository;

  public ConnectorRepositoryTests()
  {
    var services = new ServiceCollection();
    services.AddDbContext<EnterpriseIntegrationHubDbContext>(options =>
        options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}"));

    var provider = services.BuildServiceProvider();
    _context = provider.GetRequiredService<EnterpriseIntegrationHubDbContext>();
    _repository = new EnterpriseIntegrationHub.Infrastructure.Repositories.ConnectorRepository(_context);
  }

  public void Dispose()
  {
    _context.Dispose();
  }

  [Fact]
  public async Task ExistsAsync_WhenConnectorExists_ReturnsTrue()
  {
    // Arrange
    var externalSystemId = Guid.NewGuid();
    var connector = new Connector(
        "Test Connector",
        "Description",
        externalSystemId,
        "https://api.example.com",
        ConnectorProtocol.REST,
        ConnectorAuthenticationType.APIKey,
        30,
        ConnectorStatus.Active);

    _context.Connectors.Add(connector);
    await _context.SaveChangesAsync();

    // Act
    var result = await _repository.ExistsAsync(externalSystemId, ConnectorProtocol.REST, CancellationToken.None);

    // Assert
    result.Should().BeTrue();
  }

  [Fact]
  public async Task ExistsAsync_WhenConnectorDoesNotExist_ReturnsFalse()
  {
    // Arrange
    var externalSystemId = Guid.NewGuid();

    // Act
    var result = await _repository.ExistsAsync(externalSystemId, ConnectorProtocol.REST, CancellationToken.None);

    // Assert
    result.Should().BeFalse();
  }

  [Fact]
  public async Task ExistsAsync_WhenConnectorIsInactive_ReturnsFalse()
  {
    // Arrange
    var externalSystemId = Guid.NewGuid();
    var connector = new Connector(
        "Test Connector",
        "Description",
        externalSystemId,
        "https://api.example.com",
        ConnectorProtocol.REST,
        ConnectorAuthenticationType.APIKey,
        30,
        ConnectorStatus.Inactive);

    _context.Connectors.Add(connector);
    await _context.SaveChangesAsync();

    // Act
    var result = await _repository.ExistsAsync(externalSystemId, ConnectorProtocol.REST, CancellationToken.None);

    // Assert
    result.Should().BeFalse();
  }

  [Fact]
  public async Task ExistsAsync_WhenDifferentProtocol_ReturnsFalse()
  {
    // Arrange
    var externalSystemId = Guid.NewGuid();
    var connector = new Connector(
        "Test Connector",
        "Description",
        externalSystemId,
        "https://api.example.com",
        ConnectorProtocol.REST,
        ConnectorAuthenticationType.APIKey,
        30,
        ConnectorStatus.Active);

    _context.Connectors.Add(connector);
    await _context.SaveChangesAsync();

    // Act
    var result = await _repository.ExistsAsync(externalSystemId, ConnectorProtocol.SOAP, CancellationToken.None);

    // Assert
    result.Should().BeFalse();
  }

  [Fact]
  public async Task AddAsync_AddsConnectorToDatabase()
  {
    // Arrange
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

    // Act
    await _repository.AddAsync(connector, CancellationToken.None);

    // Assert
    var saved = await _context.Connectors.FindAsync(connector.Id);
    saved.Should().NotBeNull();
    saved!.Name.Should().Be("Test Connector");
    saved.ExternalSystemId.Should().Be(externalSystemId);
    saved.Protocol.Should().Be(ConnectorProtocol.REST);
    saved.Status.Should().Be(ConnectorStatus.Draft);
  }

  [Fact]
  public async Task GetAllAsync_ReturnsAllConnectorsOrderedByName()
  {
    // Arrange
    var externalSystemId = Guid.NewGuid();
    var connectors = new[]
    {
            new Connector("Zebra Connector", "Desc", externalSystemId, "https://api.example.com", ConnectorProtocol.REST, ConnectorAuthenticationType.APIKey, 30, ConnectorStatus.Active),
            new Connector("Alpha Connector", "Desc", externalSystemId, "https://api.example.com", ConnectorProtocol.REST, ConnectorAuthenticationType.APIKey, 30, ConnectorStatus.Active),
            new Connector("Beta Connector", "Desc", externalSystemId, "https://api.example.com", ConnectorProtocol.REST, ConnectorAuthenticationType.APIKey, 30, ConnectorStatus.Active)
        };

    _context.Connectors.AddRange(connectors);
    await _context.SaveChangesAsync();

    // Act
    var result = await _repository.GetAllAsync(CancellationToken.None);

    // Assert
    result.Should().HaveCount(3);
    result.Select(c => c.Name).Should().BeInAscendingOrder();
  }

  [Fact]
  public async Task GetAllAsync_WhenNoConnectors_ReturnsEmptyCollection()
  {
    // Act
    var result = await _repository.GetAllAsync(CancellationToken.None);

    // Assert
    result.Should().BeEmpty();
  }

  [Fact]
  public async Task GetByIdAsync_WhenConnectorExists_ReturnsConnector()
  {
    // Arrange
    var externalSystemId = Guid.NewGuid();
    var connector = new Connector(
        "Test Connector",
        "Description",
        externalSystemId,
        "https://api.example.com",
        ConnectorProtocol.REST,
        ConnectorAuthenticationType.APIKey,
        30,
        ConnectorStatus.Active);

    _context.Connectors.Add(connector);
    await _context.SaveChangesAsync();

    // Act
    var result = await _repository.GetByIdAsync(connector.Id, CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result!.Id.Should().Be(connector.Id);
    result.Name.Should().Be("Test Connector");
  }

  [Fact]
  public async Task GetByIdAsync_WhenConnectorDoesNotExist_ReturnsNull()
  {
    // Act
    var result = await _repository.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

    // Assert
    result.Should().BeNull();
  }

  [Fact]
  public async Task GetByIdAsync_ReturnsAsNoTracking()
  {
    // Arrange
    var externalSystemId = Guid.NewGuid();
    var connector = new Connector(
        "Test Connector",
        "Description",
        externalSystemId,
        "https://api.example.com",
        ConnectorProtocol.REST,
        ConnectorAuthenticationType.APIKey,
        30,
        ConnectorStatus.Active);

    _context.Connectors.Add(connector);
    await _context.SaveChangesAsync();

    // Act
    var result = await _repository.GetByIdAsync(connector.Id, CancellationToken.None);

    // Assert
    _context.Entry(result!).State.Should().Be(EntityState.Detached);
  }
}