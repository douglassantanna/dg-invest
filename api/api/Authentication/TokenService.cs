using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using api.Users.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace api.Authentication;
public static class AuthenticationServiceExtensions
{
    public static IServiceCollection AddTokenService(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<JWTSettings>(config.GetSection(nameof(JWTSettings)));

        var key = Encoding.ASCII.GetBytes(config.GetSection(nameof(JWTSettings)).Get<JWTSettings>()?.Secret ?? string.Empty);

        var issuer = config.GetSection(nameof(JWTSettings)).Get<JWTSettings>()?.Issuer ?? string.Empty;
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
                    ValidIssuer = issuer,
                    ValidAudience = null,
                    IssuerSigningKey = new SymmetricSecurityKey(key)
                };
            });

        services.AddSingleton<ITokenService, TokenService>();
        return services;
    }
}

public interface ITokenService
{
    string GenerateToken(User user);
}

public class TokenService : ITokenService
{
    private readonly JWTSettings _jwtSettings;

    public TokenService(IOptions<JWTSettings> jwtSettings)
    {
        _jwtSettings = jwtSettings.Value;
    }

    public string GenerateToken(User user)
    {
        var mySecurityKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_jwtSettings.Secret));

        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.FirstName),
                new Claim(ClaimTypes.Role, user.Role.ToString()),
            }),
            Expires = DateTime.UtcNow.AddDays(1),
            Issuer = _jwtSettings.Issuer,
            Audience = null,
            SigningCredentials = new SigningCredentials(mySecurityKey, SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}