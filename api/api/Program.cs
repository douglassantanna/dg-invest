using System.Reflection;
using api.Authentication;
using api.CoinMarketCap;
using api.CoinMarketCap.Service;
using api.Cryptos.Models;
using api.Cryptos.Repositories;
using api.Data;
using api.Data.Repositories;
using api.Interfaces;
using api.Models.Cryptos;
using api.RateLimiterPolicies;
using api.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<IPasswordHelper, PasswordHelper>();
builder.Services.AddScoped<ICoinMarketCapService, CoinMarketCapService>();

builder.Services.AddScoped<IBaseRepository<CryptoAsset>, CryptoAssetRepository>();
builder.Services.AddScoped<IBaseRepository<CryptoTransaction>, CryptoTransactionRepository>();
builder.Services.AddScoped<IBaseRepository<Crypto>, CryptoRepository>();

builder.Services.Configure<CoinMarketCapSettings>(builder.Configuration.GetSection(nameof(CoinMarketCapSettings)));
builder.Services.AddMediatR(Assembly.GetExecutingAssembly());
builder.Services.AddDbContext<DataContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
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

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("Policy");

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.UseRateLimiter();

app.MapControllers();

app.Run();
