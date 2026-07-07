using EnterpriseIntegrationHub.Api;
using Microsoft.AspNetCore.Builder;

var builder = WebApplication.CreateBuilder(args);

// Use Startup pattern for configuration
var startup = new Startup(builder.Configuration);
startup.ConfigureServices(builder.Services);

var app = builder.Build();

startup.Configure(app);

app.Run();
