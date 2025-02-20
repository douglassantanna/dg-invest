using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using api.Shared;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Configuration
.AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
.AddEnvironmentVariables();

var connectionString = builder.Configuration.GetValue<string>("Values:DefaultConnection");
if (string.IsNullOrEmpty(connectionString))
  throw new InvalidOperationException("Connection string 'DefaultConnection' is missing.");

builder.Services.ConfigureFunctionServices(connectionString); // Use the same extension method

builder.Build().Run();
