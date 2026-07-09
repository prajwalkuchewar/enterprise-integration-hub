using System;
using System.Threading;
using System.Threading.Tasks;
using EnterpriseIntegrationHub.Application.Features.ExternalSystems.Create;
using EnterpriseIntegrationHub.Application.Interfaces;
using EnterpriseIntegrationHub.Domain.Entities;
using EnterpriseIntegrationHub.Domain.Enums;
using FluentAssertions;
using Moq;
using Xunit;

namespace EnterpriseIntegrationHub.Application.Tests;

public class CreateExternalSystemHandlerTests
{
    [Fact]
    public async Task Handle_WhenExternalSystemExists_ThrowsInvalidOperationException()
    {
        var repo = new Mock<IExternalSystemRepository>();
        repo.Setup(r => r.ExistsAsync("name", ExternalSystemEnvironment.Development, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = new CreateExternalSystemHandler(repo.Object);
        var command = new CreateExternalSystemCommand("name", "desc", ExternalSystemEnvironment.Development);

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenNotExists_AddsEntityAndReturnsId()
    {
        var repo = new Mock<IExternalSystemRepository>();
        repo.Setup(r => r.ExistsAsync(It.IsAny<string>(), It.IsAny<ExternalSystemEnvironment>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        ExternalSystem? captured = null;
        repo.Setup(r => r.AddAsync(It.IsAny<ExternalSystem>(), It.IsAny<CancellationToken>()))
            .Callback<ExternalSystem, CancellationToken>((es, ct) => captured = es)
            .Returns(Task.CompletedTask);

        var handler = new CreateExternalSystemHandler(repo.Object);
        var command = new CreateExternalSystemCommand("name", "desc", ExternalSystemEnvironment.Testing);

        var id = await handler.Handle(command, CancellationToken.None);

        captured.Should().NotBeNull();
        id.Should().Be(captured!.Id);
        repo.Verify(r => r.AddAsync(It.IsAny<ExternalSystem>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
