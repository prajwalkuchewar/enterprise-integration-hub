using System;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace EnterpriseIntegrationHub.Api.Extensions
{
    internal static class OpenApiExtensions
    {
        public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
        {
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Enterprise Integration Hub API",
                    Version = "v1",
                    Description = "API documentation for Enterprise Integration Hub"
                });

                // Include XML comments if generated
                try
                {
                    var xmlFilename = $"{AppDomain.CurrentDomain.FriendlyName}.xml";
                    var xmlPath = Path.Combine(AppContext.BaseDirectory ?? string.Empty, xmlFilename);
                    if (!string.IsNullOrEmpty(xmlFilename) && File.Exists(xmlPath))
                    {
                        c.IncludeXmlComments(xmlPath);
                    }
                }
                catch
                {
                    // Ignore if XML comments cannot be loaded
                }
            });

            return services;
        }

        public static WebApplication UseSwaggerDocumentation(this WebApplication app)
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Enterprise Integration Hub API v1");
                c.RoutePrefix = "swagger";
            });

            return app;
        }
    }
}