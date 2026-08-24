using Maktab.Application.Abstractions;
using Maktab.Application.Services;
using Maktab.Domain.Entities;
using Maktab.Domain.Enums;

namespace Maktab.Tests;

public class UserServiceChangePasswordTests
{
    private sealed class MockUserRepository : IUserRepository
    {
        public User? GetByIdResult { get; set; }
        public User? LastUpdatedUser { get; private set; }

        public Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<User?> GetByIdAsync(int userId, CancellationToken cancellationToken = default) => Task.FromResult(GetByIdResult);
        public Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<int> CreateAsync(User user, CancellationToken cancellationToken = default) => throw new NotImplementedException();
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

    public UserServiceChangePasswordTests()
    {
        _service = new UserService(_repo, _logger);
    }

    [Fact]
    public async Task ChangePassword_WithCorrectOldPassword_UpdatesHash()
    {
        var oldHash = PasswordHasher.HashPassword("oldpass");
        var user = new User
        {
            UserId = 1,
            Username = "admin",
            PasswordHash = oldHash,
            FullName = "Admin",
            Role = UserRole.Admin,
            IsActive = true
        };
        _repo.GetByIdResult = user;

        await _service.ChangePasswordAsync(1, "oldpass", "newpass");

        Assert.NotNull(_repo.LastUpdatedUser);
        Assert.NotEqual(oldHash, _repo.LastUpdatedUser!.PasswordHash);
        Assert.True(PasswordHasher.VerifyPassword("newpass", _repo.LastUpdatedUser.PasswordHash));
    }

    [Fact]
    public async Task ChangePassword_WithWrongOldPassword_ThrowsInvalidOperationException()
    {
        var oldHash = PasswordHasher.HashPassword("oldpass");
        var user = new User
        {
            UserId = 1,
            Username = "admin",
            PasswordHash = oldHash,
            FullName = "Admin",
            Role = UserRole.Admin,
            IsActive = true
        };
        _repo.GetByIdResult = user;

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await _service.ChangePasswordAsync(1, "wrongpass", "newpass");
        });
    }

    [Fact]
    public async Task ChangePassword_WhenUserNotFound_ThrowsInvalidOperationException()
    {
        _repo.GetByIdResult = null;

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await _service.ChangePasswordAsync(999, "old", "new");
        });
    }
}
