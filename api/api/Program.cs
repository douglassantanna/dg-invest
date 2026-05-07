using System.Reflection;
using System.Text.Json.Serialization;
using api.AzureStorage.Blob;
using api.Shared;
using MediatR;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMediatR(Assembly.GetExecutingAssembly());
builder.Services.ConfiguraMemoryCache();
builder.Host.UseSerilog((context, config) => config
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("System", Serilog.Events.LogEventLevel.Warning)
    .WriteTo.Console()
    .WriteToBlobLogs(context.Configuration, "api"));
builder.Services.ConfigureJwt(builder.Configuration, builder.Environment);
builder.Services.ConfigureOptions(builder.Configuration);
builder.Services.ConfigureServices();
builder.Services.ConfigureDatabase(builder.Configuration);
builder.Services.ConfigureCustomRateLimiter(builder.Configuration);
builder.Services.ConfigureCORS(builder.Configuration);
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

app.UseCors("Policy");

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.UseRateLimiter();

app.MapControllers();

app.Run();

public partial class Program { }
