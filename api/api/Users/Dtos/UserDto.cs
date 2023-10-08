using api.Users.Models;

namespace api.Users.Dtos;
public record UserDto(
    int Id,
    string FirstName,
    string LastName,
    string Email,
    Role Role
);
