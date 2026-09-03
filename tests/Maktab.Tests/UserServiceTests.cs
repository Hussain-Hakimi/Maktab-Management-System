using Maktab.Application.Abstractions;
using Maktab.Application.Services;
using Maktab.Domain.Entities;
using Maktab.Domain.Enums;

namespace Maktab.Tests;

public class UserServiceTests
{
    private sealed class MockUserRepository : IUserRepository
    {
        public List<User> Users { get; } = [];
        public User? GetByUsernameResult { get; set; }
        public User? GetByIdResult { get; set; }
        public int LastCreatedId { get; private set; }
        public User? LastUpdatedUser { get; private set; }

        public Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default) => Task.FromResult(GetByUsernameResult);
        public Task<User?> GetByIdAsync(int userId, CancellationToken cancellationToken = default) => Task.FromResult(GetByIdResult);
        public Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<User>>(Users);
        public Task<int> CreateAsync(User user, CancellationToken cancellationToken = default)
        {
            LastCreatedId = Users.Count + 1;
            user.UserId = LastCreatedId;
            Users.Add(user);
            return Task.FromResult(LastCreatedId);
        }
        public Task UpdateAsync(User user, CancellationToken cancellationToken = default)
        {
            LastUpdatedUser = user;
            return Task.CompletedTask;
        }
        public Task DeleteAsync(int userId, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class MockLogger : IAppLogger
    {
        public void LogInfo(string message) { }
        public void LogWarning(string message) { }
        public void LogError(string message, Exception? ex = null) { }
        public Task<IReadOnlyList<string>> ReadRecentLogsAsync(int maxLines = 100, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<string>>([]);
    }

    private readonly MockUserRepository _repo = new();
    private readonly MockLogger _logger = new();
    private readonly CurrentUserService _currentUser = new();
    private readonly UserService _service;

    public UserServiceTests()
    {
        var authorizationService = new AuthorizationService(_currentUser);
        _service = new UserService(_repo, _logger, authorizationService);
    }

    private void SignInAs(int userId, UserRole role) => _currentUser.CurrentUser = new UserDto
    {
        UserId = userId,
        Username = role.ToString().ToLowerInvariant(),
        FullName = role.ToString(),
        Role = role,
        IsActive = true
    };

    [Fact]
    public async Task CreateUser_WithValidData_AsAdmin_HashesPasswordAndReturnsId()
    {
        SignInAs(1, UserRole.Admin);
        _repo.GetByUsernameResult = null;

        var dto = new SaveUserDto("teacher", "pass123", "Teacher One", UserRole.Teacher, true);
        var id = await _service.CreateUserAsync(dto);

        Assert.Equal(1, id);
        Assert.NotNull(_repo.Users.FirstOrDefault(u => u.PasswordHash != "pass123"));
    }

    [Fact]
    public async Task CreateUser_WhenUsernameExists_ThrowsInvalidOperationException()
    {
        SignInAs(1, UserRole.Admin);
        _repo.GetByUsernameResult = new User { UserId = 1, Username = "teacher", PasswordHash = "hash", FullName = "Teacher" };

        var dto = new SaveUserDto("teacher", "pass123", "Teacher One", UserRole.Teacher, true);

        await Assert.ThrowsAsync<InvalidOperationException>(async () => await _service.CreateUserAsync(dto));
    }

    [Fact]
    public async Task CreateUser_AsTeacher_IsRejectedBeforeRepositoryAccess()
    {
        SignInAs(2, UserRole.Teacher);
        var dto = new SaveUserDto("teacher", "pass123", "Teacher One", UserRole.Teacher, true);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(async () => await _service.CreateUserAsync(dto));
    }

    [Fact]
    public async Task GetAllUsers_WithoutAuthenticatedUser_IsRejected()
    {
        await Assert.ThrowsAsync<UnauthorizedAccessException>(async () => await _service.GetAllUsersAsync());
    }

    [Fact]
    public async Task ChangePassword_ForAnotherUser_AsNonAdmin_IsRejected()
    {
        SignInAs(2, UserRole.Teacher);
        _repo.GetByIdResult = new User
        {
            UserId = 1,
            Username = "admin",
            PasswordHash = PasswordHasher.HashPassword("correct"),
            FullName = "Admin",
            Role = UserRole.Admin,
            IsActive = true
        };

        await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            await _service.ChangePasswordAsync(1, "correct", "new-password"));
    }

    [Fact]
    public async Task Authenticate_WithCorrectPassword_ReturnsUserDto()
    {
        var hash = PasswordHasher.HashPassword("correct");
        _repo.GetByUsernameResult = new User
        {
            UserId = 1,
            Username = "admin",
            PasswordHash = hash,
            FullName = "Admin",
            Role = UserRole.Admin,
            IsActive = true
        };

        var result = await _service.AuthenticateAsync(new LoginDto("admin", "correct"));

        Assert.NotNull(result);
        Assert.Equal("admin", result!.Username);
    }

    [Fact]
    public async Task Authenticate_WithWrongPassword_ReturnsNull()
    {
        var hash = PasswordHasher.HashPassword("correct");
        _repo.GetByUsernameResult = new User
        {
            UserId = 1,
            Username = "admin",
            PasswordHash = hash,
            FullName = "Admin",
            Role = UserRole.Admin,
            IsActive = true
        };

        var result = await _service.AuthenticateAsync(new LoginDto("admin", "wrong"));

        Assert.Null(result);
    }

    [Fact]
    public async Task Authenticate_WhenUserInactive_ReturnsNull()
    {
        var hash = PasswordHasher.HashPassword("correct");
        _repo.GetByUsernameResult = new User
        {
            UserId = 1,
            Username = "admin",
            PasswordHash = hash,
            FullName = "Admin",
            Role = UserRole.Admin,
            IsActive = false
        };

        var result = await _service.AuthenticateAsync(new LoginDto("admin", "correct"));

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateUser_WhenPasswordEmpty_AsAdmin_KeepsExistingHash()
    {
        SignInAs(1, UserRole.Admin);
        var existingUser = new User
        {
            UserId = 1,
            Username = "admin",
            PasswordHash = "oldhash",
            FullName = "Old Name",
            Role = UserRole.Admin,
            IsActive = true
        };
        _repo.GetByIdResult = existingUser;
        _repo.GetByUsernameResult = null;

        var dto = new SaveUserDto("admin", "", "New Name", UserRole.Admin, true);
        await _service.UpdateUserAsync(1, dto);

        Assert.Equal("oldhash", _repo.LastUpdatedUser?.PasswordHash);
    }

    [Fact]
    public async Task ChangePassword_ForSelf_IsAllowed()
    {
        SignInAs(1, UserRole.Teacher);
        _repo.GetByIdResult = new User
        {
            UserId = 1,
            Username = "teacher",
            PasswordHash = PasswordHasher.HashPassword("correct"),
            FullName = "Teacher",
            Role = UserRole.Teacher,
            IsActive = true
        };

        await _service.ChangePasswordAsync(1, "correct", "new-password");

        Assert.NotEqual("correct", _repo.LastUpdatedUser?.PasswordHash);
        Assert.True(PasswordHasher.VerifyPassword("new-password", _repo.LastUpdatedUser!.PasswordHash));
    }
}
