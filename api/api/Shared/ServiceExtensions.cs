using System.Text;
using System.Threading.RateLimiting;
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
using api.Shared.Interfaces;
using api.RateLimiterPolicies;
using api.Users.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace api.Shared;
public static class ServiceExtensions
{
    public const string DefaultPolicy = "DefaultPolicy";
    public static IServiceCollection ConfigureJwt(this IServiceCollection services, IConfiguration config)
    {
        var jwtSettings = config.GetSection(nameof(JWTSettings)).Get<JWTSettings>();
        var key = Encoding.ASCII.GetBytes(jwtSettings.Secret);
        services.AddAuthentication(x =>
            {
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(x =>
            {
                x.RequireHttpsMetadata = false;
                x.SaveToken = true;
                x.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidAudience = null,
                    IssuerSigningKey = new SymmetricSecurityKey(key)
                };
            });
        return services;
    }
    public static IServiceCollection ConfigureOptions(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<JWTSettings>(config.GetSection(nameof(JWTSettings)));
        services.Configure<CoinMarketCapSettings>(config.GetSection(nameof(CoinMarketCapSettings)));
        services.Configure<AzureStorageSettings>(config.GetSection(nameof(AzureStorageSettings)));
        services.Configure<RateLimiterSettings>(config.GetSection(nameof(RateLimiterSettings)));
        return services;
    }
    public static IServiceCollection ConfigureServices(this IServiceCollection services, IConfiguration config)
    {
        services.AddScoped<IPasswordHelper, PasswordHelper>();
        services.AddScoped<ICoinMarketCapService, CoinMarketCapService>();
        services.AddScoped<IQueueService, QueueService>();
        services.AddSingleton<ITokenService, TokenService>();
        services.AddScoped<ISeedDataService, SeedDataService>();

        services.AddScoped(typeof(IBaseRepository<>), typeof(RepositoryBase<>));
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ICryptoRepository, CryptoRepository>();
        services.AddScoped<ICryptoAssetRepository, CryptoAssetRepository>();

        services.AddScoped<ITransactionService, TransactionService>();
        services.AddScoped<ITransactionStrategy, BuyTransaction>();
        services.AddScoped<ITransactionStrategy, SellTransaction>();
        services.AddScoped<ITransactionStrategy, FiatDepositTransaction>();
        services.AddScoped<ITransactionStrategy, WithdrawDepositTransaction>();
        services.AddScoped<ITransactionStrategy, CryptoDepositTransaction>();
        return services;
    }
    public static IServiceCollection ConfigureDatabase(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<DataContext>(options =>
        {
            options.UseSqlServer(config.GetConnectionString("DefaultConnection"),
            x => x.EnableRetryOnFailure());
        });
        return services;
    }
    public static IServiceCollection ConfigureCORS(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("Policy",
                policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyHeader()
                          .AllowAnyMethod();
                });
        });
        return services;
    }
    public static IServiceCollection ConfigureCustomRateLimiter(this IServiceCollection services, IConfiguration config)
    {
        var rateLimiterSettings = config.GetSection(nameof(RateLimiterSettings)).Get<RateLimiterSettings>();

        if (!int.TryParse(rateLimiterSettings.RequestsPermitLimit, out var permitLimit))
        {
            permitLimit = 320;
        }

        if (!int.TryParse(rateLimiterSettings.WindowLimitInMinutes, out var windowLimitInMinutes))
        {
            windowLimitInMinutes = 1440;
        }

        services.AddRateLimiter(options =>
        {
            options.AddFixedWindowLimiter(policyName: DefaultPolicy, options =>
            {
                options.PermitLimit = permitLimit;
                options.Window = TimeSpan.FromMinutes(windowLimitInMinutes);
                options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                options.QueueLimit = 0;
            });
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        });
        return services;
    }
    public static async Task SeedAsync(this IServiceProvider services)
    {
        using (var scope = services.CreateScope())
        {
            var seedDataService = scope.ServiceProvider.GetRequiredService<ISeedDataService>();
            await seedDataService.SeedDataAsync();
        }
    }

}
