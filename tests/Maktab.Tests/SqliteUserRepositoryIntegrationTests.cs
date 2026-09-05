using Microsoft.Data.Sqlite;
using Maktab.Domain.Entities;
using Maktab.Domain.Enums;
using Maktab.Infrastructure.Persistence;

namespace Maktab.Tests;

public class SqliteUserRepositoryIntegrationTests : IDisposable
{
    private readonly string _tempDir;
    private readonly AppFolders _folders;
    private readonly ConnectionStringProvider _connectionStringProvider;
    private readonly SqliteUserRepository _userRepository;

    public SqliteUserRepositoryIntegrationTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "MaktabUserTests_" + Guid.NewGuid());
        _folders = new AppFolders(
            Root: _tempDir,
            Data: Path.Combine(_tempDir, "Data"),
            Logs: Path.Combine(_tempDir, "Logs"),
            Backups: Path.Combine(_tempDir, "Backups"),
            Reports: Path.Combine(_tempDir, "Reports"),
            Logos: Path.Combine(_tempDir, "Logos"));

        DirectoryBootstrapper.EnsureFoldersExist(_folders);
        _connectionStringProvider = new ConnectionStringProvider(_folders);

        var initializer = new SqliteDatabaseInitializer(_connectionStringProvider);
        initializer.InitializeAsync().GetAwaiter().GetResult();

        _userRepository = new SqliteUserRepository(_connectionStringProvider);
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        try { Directory.Delete(_tempDir, recursive: true); } catch { }
    }

    [Fact]
    public async Task CreateAndRetrieveUser_Works()
    {
        var id = await _userRepository.CreateAsync(new User
        {
            Username = "teacher",
            PasswordHash = "hash",
            FullName = "Teacher One",
            Role = UserRole.Teacher,
            IsActive = true
        });

        Assert.True(id > 0);

        var user = await _userRepository.GetByUsernameAsync("teacher");
        Assert.NotNull(user);
        Assert.Equal("Teacher One", user.FullName);
        Assert.Equal(UserRole.Teacher, user.Role);
    }

    [Fact]
    public async Task CreateUser_WithDuplicateUsername_ThrowsSqliteException()
    {
        await _userRepository.CreateAsync(new User
        {
            Username = "nonexistentuser",
            PasswordHash = "hash",
            FullName = "Admin",
            Role = UserRole.Admin,
            IsActive = true
        });

        await Assert.ThrowsAsync<SqliteException>(async () =>
        {
            await _userRepository.CreateAsync(new User
            {
                Username = "nonexistentuser",
                PasswordHash = "another",
                FullName = "Admin 2",
                Role = UserRole.Admin,
                IsActive = true
            });
        });
    }

    [Fact]
    public async Task DatabaseInitialization_DoesNotCreateDefaultAdmin()
    {
        var admin = await _userRepository.GetByUsernameAsync("admin");
        Assert.Null(admin);
    }
}
