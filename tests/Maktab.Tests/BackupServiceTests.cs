using Maktab.Infrastructure.Logging;
using Maktab.Infrastructure.Persistence;

namespace Maktab.Tests;

public class BackupAndLoggingTests : IDisposable
{
    private readonly string _tempDir;
    private readonly AppFolders _folders;

    public BackupAndLoggingTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "MaktabBackupTests_" + Guid.NewGuid());
        _folders = new AppFolders(
            Root: _tempDir,
            Data: Path.Combine(_tempDir, "Data"),
            Logs: Path.Combine(_tempDir, "Logs"),
            Backups: Path.Combine(_tempDir, "Backups"),
            Reports: Path.Combine(_tempDir, "Reports"));

        DirectoryBootstrapper.EnsureFoldersExist(_folders);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, recursive: true);
            }
        }
        catch
        {
            // Ignore cleanup failure in temp
        }
    }

    [Fact]
    public async Task FileAppLogger_WritesAndReadsLogsSuccessfully()
    {
        var logger = new FileAppLogger(_folders);

        logger.LogInfo("Test info message");
        logger.LogWarning("Test warning message");
        logger.LogError("Test error message", new InvalidOperationException("Something went wrong"));

        var logs = await logger.ReadRecentLogsAsync(10);

        Assert.NotEmpty(logs);
        Assert.Contains(logs, l => l.Contains("Test info message"));
        Assert.Contains(logs, l => l.Contains("Test error message"));
    }

    [Fact]
    public async Task PruneOldBackups_RemovesOnlyFilesOlderThanRetentionDays()
    {
        var logger = new FileAppLogger(_folders);
        var backupService = new SqliteBackupService(_folders, new ConnectionStringProvider(_folders), logger);

        var oldBackup = Path.Combine(_folders.Backups, "maktab_backup_20200101_000000.db");
        var recentBackup = Path.Combine(_folders.Backups, "maktab_backup_20260816_120000.db");

        await File.WriteAllTextAsync(oldBackup, "dummy old db");
        await File.WriteAllTextAsync(recentBackup, "dummy recent db");

        File.SetCreationTime(oldBackup, DateTime.Now.AddDays(-15));
        File.SetCreationTime(recentBackup, DateTime.Now);

        await backupService.PruneOldBackupsAsync(7);

        Assert.False(File.Exists(oldBackup));
        Assert.True(File.Exists(recentBackup));

        var list = await backupService.GetBackupsListAsync();
        Assert.Single(list);
        Assert.Equal("maktab_backup_20260816_120000.db", list[0].FileName);
    }
}
