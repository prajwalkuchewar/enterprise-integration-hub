using EnterpriseIntegrationHub.Api.Extensions;
using EnterpriseIntegrationHub.Application.Features.ExternalSystems.Create;
using EnterpriseIntegrationHub.Application.Interfaces;
using EnterpriseIntegrationHub.Infrastructure.Persistence;
using EnterpriseIntegrationHub.Infrastructure.Repositories;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseIntegrationHub.Api
{
    internal sealed class Startup
    {
        private const string DefaultCorsPolicy = "Default";
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            // Core
            services.AddControllers();

            // OpenAPI / Swagger
            services.AddSwaggerDocumentation();

            // Persistence
            services.AddDbContext<EnterpriseIntegrationHubDbContext>(options =>
                options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")));

            // Application services / DI
            services.AddScoped<CreateExternalSystemHandler>();
            services.AddScoped<IExternalSystemRepository, ExternalSystemRepository>();

            // Cross-origin requests (adjust origins as needed)
            services.AddCors(options =>
            {
                options.AddPolicy(DefaultCorsPolicy, builder =>
                {
                    builder.AllowAnyHeader()
                           .AllowAnyMethod()
                           .AllowAnyOrigin();
                });
            });
        }

        public void Configure(WebApplication app)
        {
            // OpenAPI (only in Development by default, controlled in Program.cs)
            if (app.Environment.IsDevelopment())
            {
                app.UseSwaggerDocumentation();
            }

            // Middleware
            app.UseRouting();
            app.UseCors(DefaultCorsPolicy);

            // Endpoints
            app.MapControllers();
        }
    }
}