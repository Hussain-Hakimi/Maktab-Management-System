using Maktab.Application.Abstractions;
using Maktab.Application.Services;
using Maktab.Domain.Entities;
using Maktab.Domain.Enums;
using Moq;

namespace Maktab.Tests;

public class UserServiceChangePasswordTests
{
    private readonly Mock<IUserRepository> _repoMock = new();
    private readonly Mock<IAppLogger> _loggerMock = new();
    private readonly UserService _service;

    public UserServiceChangePasswordTests()
    {
        _service = new UserService(_repoMock.Object, _loggerMock.Object);
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
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        await _service.ChangePasswordAsync(1, "oldpass", "newpass");

        _repoMock.Verify(r => r.UpdateAsync(
            It.Is<User>(u => u.PasswordHash != oldHash && PasswordHasher.VerifyPassword("newpass", u.PasswordHash)),
            It.IsAny<CancellationToken>()), Times.Once);
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
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await _service.ChangePasswordAsync(1, "wrongpass", "newpass");
        });
    }

    [Fact]
    public async Task ChangePassword_WhenUserNotFound_ThrowsInvalidOperationException()
    {
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await _service.ChangePasswordAsync(999, "old", "new");
        });
    }
}
