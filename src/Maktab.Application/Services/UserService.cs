using Maktab.Application.Abstractions;
using Maktab.Domain.Entities;
using Maktab.Domain.Enums;

namespace Maktab.Application.Services;

public sealed class UserService(
    IUserRepository repository,
    IAppLogger logger,
    IAuthorizationService authorizationService) : IUserService
{
    public async Task<UserDto?> AuthenticateAsync(LoginDto login, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(login.Username) || string.IsNullOrWhiteSpace(login.Password))
            return null;

        var user = await repository.GetByUsernameAsync(login.Username.Trim(), cancellationToken);
        if (user is null || !user.IsActive || !PasswordHasher.VerifyPassword(login.Password, user.PasswordHash))
        {
            logger.LogWarning($"Failed login attempt for username '{login.Username}'.");
            return null;
        }

        logger.LogInfo($"User '{user.Username}' logged in successfully.");
        return MapToDto(user);
    }

    public async Task<IReadOnlyList<UserDto>> GetAllUsersAsync(CancellationToken cancellationToken = default)
    {
        authorizationService.RequireRole(UserRole.Admin);

        var users = await repository.GetAllAsync(cancellationToken);
        return users.Select(MapToDto).ToList();
    }

    public async Task<int> CreateUserAsync(SaveUserDto user, CancellationToken cancellationToken = default)
    {
        authorizationService.RequireRole(UserRole.Admin);
        ValidateUser(user);

        var existing = await repository.GetByUsernameAsync(user.Username.Trim(), cancellationToken);
        if (existing is not null)
            throw new InvalidOperationException("Username already exists.");

        var entity = new User
        {
            Username = user.Username.Trim(),
            PasswordHash = PasswordHasher.HashPassword(user.Password),
            FullName = user.FullName.Trim(),
            Role = user.Role,
            IsActive = user.IsActive
        };

        var id = await repository.CreateAsync(entity, cancellationToken);
        logger.LogInfo($"New user '{entity.Username}' created.");
        return id;
    }

    public async Task UpdateUserAsync(int userId, SaveUserDto user, CancellationToken cancellationToken = default)
    {
        authorizationService.RequireRole(UserRole.Admin);
        if (userId <= 0) throw new ArgumentOutOfRangeException(nameof(userId));
        ValidateUser(user, isUpdate: true);

        var existingUser = await repository.GetByIdAsync(userId, cancellationToken);
        if (existingUser is null)
            throw new InvalidOperationException("User not found.");

        if (!string.Equals(existingUser.Username, user.Username.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            var conflict = await repository.GetByUsernameAsync(user.Username.Trim(), cancellationToken);
            if (conflict is not null && conflict.UserId != userId)
                throw new InvalidOperationException("Username already exists.");
        }

        var updated = new User
        {
            UserId = userId,
            Username = user.Username.Trim(),
            PasswordHash = string.IsNullOrWhiteSpace(user.Password)
                ? existingUser.PasswordHash
                : PasswordHasher.HashPassword(user.Password),
            FullName = user.FullName.Trim(),
            Role = user.Role,
            IsActive = user.IsActive
        };

        await repository.UpdateAsync(updated, cancellationToken);
        logger.LogInfo($"User '{updated.Username}' updated.");
    }

    public async Task DeleteUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        authorizationService.RequireRole(UserRole.Admin);
        if (userId <= 0) throw new ArgumentOutOfRangeException(nameof(userId));
        await repository.DeleteAsync(userId, cancellationToken);
        logger.LogInfo($"User ID {userId} deleted.");
    }

    public async Task ChangePasswordAsync(int userId, string oldPassword, string newPassword, CancellationToken cancellationToken = default)
    {
        authorizationService.RequireSelfOrRole(userId, UserRole.Admin);
        if (string.IsNullOrWhiteSpace(oldPassword)) throw new ArgumentException("Old password is required.", nameof(oldPassword));
        if (string.IsNullOrWhiteSpace(newPassword)) throw new ArgumentException("New password is required.", nameof(newPassword));

        var user = await repository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
            throw new InvalidOperationException("User not found.");

        if (!PasswordHasher.VerifyPassword(oldPassword, user.PasswordHash))
            throw new InvalidOperationException("Old password is incorrect.");

        user.PasswordHash = PasswordHasher.HashPassword(newPassword);
        await repository.UpdateAsync(user, cancellationToken);
        logger.LogInfo($"User '{user.Username}' changed password.");
    }

    private static void ValidateUser(SaveUserDto user, bool isUpdate = false)
    {
        if (string.IsNullOrWhiteSpace(user.Username))
            throw new ArgumentException("Username is required.", nameof(user.Username));
        if (!isUpdate && string.IsNullOrWhiteSpace(user.Password))
            throw new ArgumentException("Password is required for new user.", nameof(user.Password));
        if (string.IsNullOrWhiteSpace(user.FullName))
            throw new ArgumentException("Full name is required.", nameof(user.FullName));
    }

    private static UserDto MapToDto(User user) => new()
    {
        UserId = user.UserId,
        Username = user.Username,
        FullName = user.FullName,
        Role = user.Role,
        IsActive = user.IsActive
    };
}
