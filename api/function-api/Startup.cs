using System;
using System.Reflection;
using FluentValidation.AspNetCore;
using function_api.Data;
using function_api.Interfaces;
using function_api.Shared;
using MediatR;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(function_api.Startup))]

namespace function_api;
public class Startup : FunctionsStartup
{
    public override void Configure(IFunctionsHostBuilder builder)
    {
        builder.Services.AddScoped<IPasswordHelper, PasswordHelper>();
        builder.Services.AddMediatR(Assembly.GetExecutingAssembly());
        builder.Services.AddFluentValidationAutoValidation().AddFluentValidationClientsideAdapters();
        string connectionString = Environment.GetEnvironmentVariable("ConnectionString") ?? throw new Exception("ConnectionString not found");

        builder.Services.AddDbContext<DataContext>(
            options => SqlServerDbContextOptionsExtensions.UseSqlServer(options, connectionString));
    }
}
