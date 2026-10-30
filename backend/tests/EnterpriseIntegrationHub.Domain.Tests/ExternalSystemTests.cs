using System;
using EnterpriseIntegrationHub.Domain.Entities;
using EnterpriseIntegrationHub.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace EnterpriseIntegrationHub.Domain.Tests;

public sealed class ExternalSystemTests
{
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
}
