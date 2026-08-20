using Maktab.Domain.Enums;

namespace Maktab.Application.Abstractions;

public sealed class UserDto
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsActive { get; set; }
}

public sealed record SaveUserDto(
    string Username,
    string Password,
    string FullName,
    UserRole Role,
    bool IsActive);

public sealed record LoginDto(
    string Username,
    string Password);
