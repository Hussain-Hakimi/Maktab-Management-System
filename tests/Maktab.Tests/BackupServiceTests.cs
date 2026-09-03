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
            Reports: Path.Combine(_tempDir, "Reports"),
            Logos: Path.Combine(_tempDir, "Logos"));

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
    public async Task PruneOldBackups_KeepsDailyBackupsFor30Days()
    {
        var logger = new FileAppLogger(_folders);
        var backupService = new SqliteBackupService(_folders, new ConnectionStringProvider(_folders), logger);
        var now = DateTime.Now;

        var recentBackup = Path.Combine(_folders.Backups, "maktab_backup_recent.db");
        var oldBackup = Path.Combine(_folders.Backups, "maktab_backup_old.db");

        await File.WriteAllTextAsync(recentBackup, "dummy recent db");
        await File.WriteAllTextAsync(oldBackup, "dummy old db");

        File.SetCreationTime(recentBackup, now.AddDays(-29));
        File.SetCreationTime(oldBackup, now.AddDays(-31));

        await backupService.PruneOldBackupsAsync();

        Assert.True(File.Exists(recentBackup));
        Assert.False(File.Exists(oldBackup));
    }

    [Fact]
    public async Task PruneOldBackups_KeepsOneBackupPerWeekForOlderBackups()
    {
        var logger = new FileAppLogger(_folders);
        var backupService = new SqliteBackupService(_folders, new ConnectionStringProvider(_folders), logger);
        var now = DateTime.Now.Date;
        var daysFromMonday = ((int)now.DayOfWeek + 6) % 7;
        var currentWeekStart = now.AddDays(-daysFromMonday);

        var weekOneNewest = Path.Combine(_folders.Backups, "maktab_backup_week1_newest.db");
        var weekOneOlder = Path.Combine(_folders.Backups, "maktab_backup_week1_older.db");
        var weekTwo = Path.Combine(_folders.Backups, "maktab_backup_week2.db");
        var tooOld = Path.Combine(_folders.Backups, "maktab_backup_too_old.db");

        await File.WriteAllTextAsync(weekOneNewest, "dummy db");
        await File.WriteAllTextAsync(weekOneOlder, "dummy db");
        await File.WriteAllTextAsync(weekTwo, "dummy db");
        await File.WriteAllTextAsync(tooOld, "dummy db");

        File.SetCreationTime(weekOneNewest, currentWeekStart.AddDays(-42).AddDays(4));
        File.SetCreationTime(weekOneOlder, currentWeekStart.AddDays(-42).AddDays(1));
        File.SetCreationTime(weekTwo, currentWeekStart.AddDays(-49).AddDays(3));
        File.SetCreationTime(tooOld, currentWeekStart.AddDays(-26 * 7));

        await backupService.PruneOldBackupsAsync();

        Assert.True(File.Exists(weekOneNewest));
        Assert.False(File.Exists(weekOneOlder));
        Assert.True(File.Exists(weekTwo));
        Assert.False(File.Exists(tooOld));
    }

    [Fact]
    public async Task PruneOldBackups_DoesNotDeletePreRestoreSafetyBackups()
    {
        var logger = new FileAppLogger(_folders);
        var backupService = new SqliteBackupService(_folders, new ConnectionStringProvider(_folders), logger);

        var safetyBackup = Path.Combine(_folders.Backups, "maktab_pre_restore_20200101_000000_000.db");
        await File.WriteAllTextAsync(safetyBackup, "safety db");
        File.SetCreationTime(safetyBackup, DateTime.Now.AddDays(-365));

        await backupService.PruneOldBackupsAsync();

        Assert.True(File.Exists(safetyBackup));
    }
}
