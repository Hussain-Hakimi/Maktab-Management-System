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
        public Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
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
        public Task DeleteAsync(int userId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private sealed class MockLogger : IAppLogger
    {
        public void LogInfo(string message) { }
        public void LogWarning(string message) { }
        public void LogError(string message, Exception? ex = null) { }
        public Task<IReadOnlyList<string>> ReadRecentLogsAsync(int maxLines = 100, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private readonly MockUserRepository _repo = new();
    private readonly MockLogger _logger = new();
    private readonly UserService _service;

    public UserServiceTests()
    {
        _service = new UserService(_repo, _logger);
    }

    [Fact]
    public async Task CreateUser_WithValidData_HashesPasswordAndReturnsId()
    {
        _repo.GetByUsernameResult = null;

        var dto = new SaveUserDto("teacher", "pass123", "Teacher One", UserRole.Teacher, true);
        var id = await _service.CreateUserAsync(dto);

        Assert.Equal(1, id);
        Assert.NotNull(_repo.Users.FirstOrDefault(u => u.PasswordHash != "pass123"));
    }

    [Fact]
    public async Task CreateUser_WhenUsernameExists_ThrowsInvalidOperationException()
    {
        _repo.GetByUsernameResult = new User { UserId = 1, Username = "teacher", PasswordHash = "hash", FullName = "Teacher" };

        var dto = new SaveUserDto("teacher", "pass123", "Teacher One", UserRole.Teacher, true);

        await Assert.ThrowsAsync<InvalidOperationException>(async () => await _service.CreateUserAsync(dto));
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
    public async Task UpdateUser_WhenPasswordEmpty_KeepsExistingHash()
    {
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
}
