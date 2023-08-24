using System.Text.Json.Serialization;
using function_api.Auth.Models;

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
}