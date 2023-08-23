using System;
using System.Text.Json.Serialization;
using function_api.Users.Models;

namespace function_api.Auth.Models;
public class ApiKey
{
    public int Id { get; set; }
    public string Key { get; set; }
    public int UserId { get; set; }
    [JsonIgnore]
    public User User { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsExpired() => ExpiresAt < DateTime.UtcNow;
}