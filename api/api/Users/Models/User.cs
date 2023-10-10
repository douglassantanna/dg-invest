using System.Text.Json.Serialization;

namespace api.Users.Models;
public class User
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    [JsonIgnore]
    public string Password { get; set; } = string.Empty;
    public Role Role { get; set; }
    [JsonIgnore]
    public string? ApiKey { get; set; }
}