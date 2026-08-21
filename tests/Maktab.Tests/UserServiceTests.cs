using Maktab.Application.Abstractions;
using Maktab.Application.Services;
using Maktab.Domain.Entities;
using Maktab.Domain.Enums;
using Moq;

namespace Maktab.Tests;

public class UserServiceTests
{
    private readonly Mock<IUserRepository> _repoMock = new();
    private readonly Mock<IAppLogger> _loggerMock = new();
    private readonly UserService _service;

    public UserServiceTests()
    {
        _service = new UserService(_repoMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task CreateUser_WithValidData_HashesPasswordAndReturnsId()
    {
        _repoMock.Setup(r => r.GetByUsernameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync((User?)null);
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(1);

        var dto = new SaveUserDto("teacher", "pass123", "Teacher One", UserRole.Teacher, true);
        var id = await _service.CreateUserAsync(dto);

        Assert.Equal(1, id);
        _repoMock.Verify(r => r.CreateAsync(It.Is<User>(u => u.PasswordHash != "pass123"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateUser_WhenUsernameExists_ThrowsInvalidOperationException()
    {
        var existing = new User
        {
            UserId = 1,
            Username = "teacher",
            PasswordHash = "hash",
            FullName = "Existing",
            Role = UserRole.Teacher,
            IsActive = true
        };
        _repoMock.Setup(r => r.GetByUsernameAsync("teacher", It.IsAny<CancellationToken>()))
                 .ReturnsAsync(existing);

        var dto = new SaveUserDto("teacher", "pass123", "Teacher One", UserRole.Teacher, true);

        await Assert.ThrowsAsync<InvalidOperationException>(async () => await _service.CreateUserAsync(dto));
    }

    [Fact]
    public async Task Authenticate_WithCorrectPassword_ReturnsUserDto()
    {
        var hash = PasswordHasher.HashPassword("correct");
        var user = new User
        {
            UserId = 1,
            Username = "admin",
            PasswordHash = hash,
            FullName = "Admin",
            Role = UserRole.Admin,
            IsActive = true
        };
        _repoMock.Setup(r => r.GetByUsernameAsync("admin", It.IsAny<CancellationToken>()))
                 .ReturnsAsync(user);

        var result = await _service.AuthenticateAsync(new LoginDto("admin", "correct"));

        Assert.NotNull(result);
        Assert.Equal("admin", result.Username);
    }

    [Fact]
    public async Task Authenticate_WithWrongPassword_ReturnsNull()
    {
        var hash = PasswordHasher.HashPassword("correct");
        var user = new User
        {
            UserId = 1,
            Username = "admin",
            PasswordHash = hash,
            FullName = "Admin",
            Role = UserRole.Admin,
            IsActive = true
        };
        _repoMock.Setup(r => r.GetByUsernameAsync("admin", It.IsAny<CancellationToken>()))
                 .ReturnsAsync(user);

        var result = await _service.AuthenticateAsync(new LoginDto("admin", "wrong"));

        Assert.Null(result);
    }

    [Fact]
    public async Task Authenticate_WhenUserInactive_ReturnsNull()
    {
        var hash = PasswordHasher.HashPassword("correct");
        var user = new User
        {
            UserId = 1,
            Username = "admin",
            PasswordHash = hash,
            FullName = "Admin",
            Role = UserRole.Admin,
            IsActive = false
        };
        _repoMock.Setup(r => r.GetByUsernameAsync("admin", It.IsAny<CancellationToken>()))
                 .ReturnsAsync(user);

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
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(existingUser);
        _repoMock.Setup(r => r.GetByUsernameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync((User?)null);

        var dto = new SaveUserDto("admin", "", "New Name", UserRole.Admin, true);
        await _service.UpdateUserAsync(1, dto);

        _repoMock.Verify(r => r.UpdateAsync(It.Is<User>(u => u.PasswordHash == "oldhash"), It.IsAny<CancellationToken>()), Times.Once);
    }
}
