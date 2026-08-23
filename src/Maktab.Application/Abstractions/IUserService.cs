namespace Maktab.Application.Abstractions;

public interface IUserService
{
    Task<UserDto?> AuthenticateAsync(LoginDto login, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserDto>> GetAllUsersAsync(CancellationToken cancellationToken = default);
    Task<int> CreateUserAsync(SaveUserDto user, CancellationToken cancellationToken = default);
    Task UpdateUserAsync(int userId, SaveUserDto user, CancellationToken cancellationToken = default);
    Task DeleteUserAsync(int userId, CancellationToken cancellationToken = default);
    Task ChangePasswordAsync(int userId, string oldPassword, string newPassword, CancellationToken cancellationToken = default);
}
