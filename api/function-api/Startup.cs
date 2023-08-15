using System;
using System.Reflection;
using FluentValidation.AspNetCore;
using function_api.Data;
using MediatR;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(function_api.Startup))]

namespace function_api;
class Startup : FunctionsStartup
{
    public override void Configure(IFunctionsHostBuilder builder)
    {
        builder.Services.AddMediatR(Assembly.GetExecutingAssembly());
        builder.Services.AddFluentValidationAutoValidation().AddFluentValidationClientsideAdapters();
        string connectionString = Environment.GetEnvironmentVariable("SqlConnectionString");
        builder.Services.AddDbContext<DataContext>(
            options => SqlServerDbContextOptionsExtensions.UseSqlServer(options, connectionString));
    }
}
