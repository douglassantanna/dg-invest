namespace api.Authentication;
public class JWTSettings
{
    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public int ExpiryMinutes { get; set; }
}
