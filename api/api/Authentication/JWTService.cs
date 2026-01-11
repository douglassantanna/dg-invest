using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using api.Users.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace api.Authentication;
public interface IJWTService
{
    string GenerateJWT(User user);
}
public class JWTService : IJWTService
{
    private readonly JWTSettings _jwtSettings;
    public JWTService(IOptions<JWTSettings> jwtSettings)
    {
        _jwtSettings = jwtSettings.Value;
    }
    public string GenerateJWT(User user)
    {
        var mySecurityKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_jwtSettings.Secret));

        var jwtHandler = new JwtSecurityTokenHandler();
        var jwtDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new Claim[]
            {
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.FullName),
                new(ClaimTypes.Role, user.Role.ToString()),
            }),
            Expires = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes),
            Issuer = _jwtSettings.Issuer,
            Audience = null,
            SigningCredentials = new SigningCredentials(mySecurityKey, SecurityAlgorithms.HmacSha256Signature)
        };

        var jwt = jwtHandler.CreateToken(jwtDescriptor);
        return jwtHandler.WriteToken(jwt);
    }
}