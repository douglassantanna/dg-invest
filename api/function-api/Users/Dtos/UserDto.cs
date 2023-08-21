using function_api.Users.Models;

namespace function_api.Users.Dtos;
public record UserDto(
    int Id,
    string FirstName,
    string LastName,
    string Email,
    Role Role
);
