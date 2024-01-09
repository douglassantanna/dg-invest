using api.Users.Models;

namespace api.Users.Dtos;
public record UserDto(
    int Id,
    string FullName,
    string Email,
    Role Role
);
