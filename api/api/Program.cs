using System.Reflection;
using api.Authentication;
using api.AzureStorage;
using api.AzureStorage.Queue;
using api.CoinMarketCap;
using api.CoinMarketCap.Service;
using api.Cryptos.Repositories;
using api.Cryptos.TransactionStrategies;
using api.Cryptos.TransactionStrategies.Contracts;
using api.Cryptos.TransactionStrategies.Transactions;
using api.Data;
using api.Data.Repositories;
using api.Interfaces;
using api.RateLimiterPolicies;
using api.Shared;
using api.Users.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<IPasswordHelper, PasswordHelper>();
builder.Services.AddScoped<ICoinMarketCapService, CoinMarketCapService>();
builder.Services.AddScoped<IQueueService, QueueService>();

builder.Services.AddScoped(typeof(IBaseRepository<>), typeof(RepositoryBase<>));
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ICryptoRepository, CryptoRepository>();
builder.Services.AddScoped<ICryptoAssetRepository, CryptoAssetRepository>();
builder.Services.AddScoped<ITransactionStrategy, BuyTransaction>();
builder.Services.AddScoped<ITransactionService, TransactionService>();

builder.Services.Configure<CoinMarketCapSettings>(builder.Configuration.GetSection(nameof(CoinMarketCapSettings)));
builder.Services.Configure<AzureStorageSettings>(builder.Configuration.GetSection(nameof(AzureStorageSettings)));

builder.Services.AddMediatR(Assembly.GetExecutingAssembly());
builder.Services.AddDbContext<DataContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
    x => x.EnableRetryOnFailure());
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("Policy",
        policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});

builder.Host.UseSerilog((context, config) =>
    config
    .ReadFrom.Configuration(context.Configuration));

builder.Services.AddTokenService(builder.Configuration);

builder.Services.AddCustomRateLimiter(builder.Configuration);

builder.Services.AddControllers(opt =>
{
    opt.Filters.Add<GlobalExceptionFilter>();
});

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    DatabaseManagementService.MigrationInitialization(app);
}

app.UseCors("Policy");

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.UseRateLimiter();

app.MapControllers();

app.Run();
