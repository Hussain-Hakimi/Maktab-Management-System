using Maktab.Infrastructure.Logging;
using Maktab.Infrastructure.Persistence;

namespace Maktab.Tests;

public class BackupServiceLastBackupTests : IDisposable
{
    private readonly string _tempDir;
    private readonly AppFolders _folders;
    private readonly SqliteBackupService _backupService;

    public BackupServiceLastBackupTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "MaktabBackupLastBackupTests_" + Guid.NewGuid());
        _folders = new AppFolders(
            Root: _tempDir,
            Data: Path.Combine(_tempDir, "Data"),
            Logs: Path.Combine(_tempDir, "Logs"),
            Backups: Path.Combine(_tempDir, "Backups"),
            Reports: Path.Combine(_tempDir, "Reports"),
            Logos: Path.Combine(_tempDir, "Logos"));

        DirectoryBootstrapper.EnsureFoldersExist(_folders);

        var connectionStringProvider = new ConnectionStringProvider(_folders);
        var initializer = new SqliteDatabaseInitializer(connectionStringProvider);
        initializer.InitializeAsync().GetAwaiter().GetResult();

        var logger = new FileAppLogger(_folders);
        _backupService = new SqliteBackupService(_folders, connectionStringProvider, logger);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); } catch { }
    }

    [Fact]
    public async Task GetLastBackupDate_WhenNoBackups_ReturnsNull()
    {
        var result = await _backupService.GetLastBackupDateAsync();

        Assert.Null(result);
    }

    [Fact]
    public async Task GetLastBackupDate_AfterCreatingBackup_ReturnsDate()
    {
        await _backupService.CreateBackupAsync();

        var result = await _backupService.GetLastBackupDateAsync();

        Assert.NotNull(result);
        Assert.True((DateTime.Now - result.Value).TotalMinutes < 5);
    }

    [Fact]
    public async Task GetRemovableDrivePaths_ReturnsList()
    {
        // This test may return an empty list on systems without USB drives.
        // We verify that the method completes without exception.
        var drives = await _backupService.GetRemovableDrivePathsAsync();

        Assert.NotNull(drives);
    }
}
