using System;
using EnterpriseIntegrationHub.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EnterpriseIntegrationHub.Api.Tests;

internal sealed class EnterpriseIntegrationHubApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"ApiHostTests_{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<EnterpriseIntegrationHubDbContext>();
            services.RemoveAll<DbContextOptions<EnterpriseIntegrationHubDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<EnterpriseIntegrationHubDbContext>>();
            services.AddDbContext<EnterpriseIntegrationHubDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));
        });
    }
}

