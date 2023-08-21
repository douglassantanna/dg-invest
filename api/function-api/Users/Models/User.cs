using System.Text.Json.Serialization;

namespace function_api.Users.Models;
public class User
{
    public int Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    [JsonIgnore]
    public string Password { get; set; }
    public Role Role { get; set; }
    [JsonIgnore]
    public string ApiKey { get; set; }
}