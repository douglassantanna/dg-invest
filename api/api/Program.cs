using System.Reflection;
using System.Text.Json.Serialization;
using api.Services;
using api.Shared;
using MediatR;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMediatR(Assembly.GetExecutingAssembly());
builder.Services.ConfiguraMemoryCache();
builder.Services.AddHostedService<MarketDataBackgroundService>();
builder.Host.UseSerilog((context, config) => config.ReadFrom.Configuration(context.Configuration));
builder.Services.ConfigureJwt(builder.Configuration);
builder.Services.ConfigureOptions(builder.Configuration);
builder.Services.ConfigureServices();
builder.Services.ConfigureDatabase(builder.Configuration);
builder.Services.ConfigureCustomRateLimiter(builder.Configuration);
builder.Services.ConfigureCORS();
builder.Services.AddControllers(opt =>
{
    opt.Filters.Add<GlobalExceptionFilter>();
}).AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// var runMigrationsConfig = app.Configuration.GetSection("RunMigrations")?.Value;
// bool runMigrations = false;
// if (!string.IsNullOrEmpty(runMigrationsConfig)
//     && bool.TryParse(runMigrationsConfig, out runMigrations)
//     && runMigrations)
// {
//     await app.Services.SeedAsync();
// }

app.UseCors("Policy");

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.UseRateLimiter();

app.MapControllers();

app.Run();
