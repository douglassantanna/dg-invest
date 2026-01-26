using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using api.Shared;
using Microsoft.Extensions.DependencyInjection;
using functions;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Configuration
.AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
.AddEnvironmentVariables();
builder.Services.ConfigureFunctionServices(builder.Configuration); // Use the same extension method
builder.Services.AddHttpClient<HealthCheck>();

builder.Build().Run();
