using System;
using EnterpriseIntegrationHub.Domain.Entities;
using EnterpriseIntegrationHub.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace EnterpriseIntegrationHub.Domain.Tests;

public sealed class ExternalSystemTests
{
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_WithBlankName_Throws(string name)
    {
        var action = () => new ExternalSystem(name, "Orders API", ExternalSystemEnvironment.Production, ExternalSystemStatus.Active);

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_WithBlankDescription_Throws()
    {
        var action = () => new ExternalSystem("Orders", " ", ExternalSystemEnvironment.Production, ExternalSystemStatus.Active);

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_WithUndefinedEnvironment_Throws()
    {
        var action = () => new ExternalSystem("Orders", "Orders API", (ExternalSystemEnvironment)999, ExternalSystemStatus.Active);

        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Constructor_WithUndefinedStatus_Throws()
    {
        var action = () => new ExternalSystem("Orders", "Orders API", ExternalSystemEnvironment.Production, (ExternalSystemStatus)999);

        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Deactivate_ActiveSystem_MarksItInactive()
    {
        var system = new ExternalSystem("Orders", "Orders API", ExternalSystemEnvironment.Production, ExternalSystemStatus.Active);

        system.Deactivate();

        system.Status.Should().Be(ExternalSystemStatus.Inactive);
        system.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Activate_InactiveSystem_MarksItActive()
    {
        var system = new ExternalSystem("Orders", "Orders API", ExternalSystemEnvironment.Production, ExternalSystemStatus.Inactive);

        system.Activate();

        system.Status.Should().Be(ExternalSystemStatus.Active);
    }

    [Fact]
    public void Update_EmptyName_Throws()
    {
        var system = new ExternalSystem("Orders", "Orders API", ExternalSystemEnvironment.Production, ExternalSystemStatus.Active);

        var action = () => system.Update(" ", "Updated", ExternalSystemEnvironment.Testing);

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Update_WithUndefinedEnvironment_Throws()
    {
        var system = new ExternalSystem("Orders", "Orders API", ExternalSystemEnvironment.Production, ExternalSystemStatus.Active);

        var action = () => system.Update("Orders", "Updated", (ExternalSystemEnvironment)999);

        action.Should().Throw<ArgumentOutOfRangeException>();
    }
}
