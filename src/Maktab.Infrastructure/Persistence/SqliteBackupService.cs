using Microsoft.Data.Sqlite;
using Maktab.Application.Abstractions;

namespace Maktab.Infrastructure.Persistence;

public sealed class SqliteBackupService(
    AppFolders folders,
    IConnectionStringProvider connectionStringProvider,
    IAppLogger logger) : IBackupService
{
    public async Task<string> CreateBackupAsync(CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(folders.Backups);

        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var backupFileName = $"maktab_backup_{timestamp}.db";
        var backupFilePath = Path.Combine(folders.Backups, backupFileName);

        var destBuilder = new SqliteConnectionStringBuilder
        {
            DataSource = backupFilePath,
            Mode = SqliteOpenMode.ReadWriteCreate
        };

        try
        {
            await using (var sourceConnection = new SqliteConnection(connectionStringProvider.GetConnectionString()))
            await using (var destConnection = new SqliteConnection(destBuilder.ToString()))
            {
                await sourceConnection.OpenAsync(cancellationToken);
                await destConnection.OpenAsync(cancellationToken);

                // Checkpoint WAL to flush all active transactions before backup
                await using (var checkpointCmd = sourceConnection.CreateCommand())
                {
                    checkpointCmd.CommandText = "PRAGMA wal_checkpoint(TRUNCATE);";
                    await checkpointCmd.ExecuteNonQueryAsync(cancellationToken);
                }

                sourceConnection.BackupDatabase(destConnection);
            }

            logger.LogInfo($"Backup created successfully at: {backupFilePath}");

            // Auto prune old backups
            await PruneOldBackupsAsync(7, cancellationToken);

            return backupFilePath;
        }
        catch (Exception ex)
        {
            logger.LogError($"Failed to create backup: {ex.Message}", ex);
            throw;
        }
    }

    public Task<IReadOnlyList<BackupInfoDto>> GetBackupsListAsync(CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(folders.Backups);

        var dirInfo = new DirectoryInfo(folders.Backups);
        var files = dirInfo.GetFiles("*.db")
            .OrderByDescending(f => f.CreationTime)
            .Select(f => new BackupInfoDto(
                FileName: f.Name,
                FilePath: f.FullName,
                FileSizeBytes: f.Length,
                FileSizeFormatted: FormatFileSize(f.Length),
                CreatedAt: f.CreationTime,
                CreatedAtFormatted: f.CreationTime.ToString("yyyy/MM/dd HH:mm:ss")
            )).ToList();

        return Task.FromResult<IReadOnlyList<BackupInfoDto>>(files);
    }

    public async Task RestoreBackupAsync(string backupFilePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(backupFilePath))
        {
            throw new FileNotFoundException("فایل نسخه پشتیبان یافت نشد.", backupFilePath);
        }

        try
        {
            SqliteConnection.ClearAllPools();

            var mainDbPath = Path.Combine(folders.Data, "maktab.db");
            var mainDbWal = Path.Combine(folders.Data, "maktab.db-wal");
            var mainDbShm = Path.Combine(folders.Data, "maktab.db-shm");

            if (File.Exists(mainDbWal)) File.Delete(mainDbWal);
            if (File.Exists(mainDbShm)) File.Delete(mainDbShm);

            File.Copy(backupFilePath, mainDbPath, overwrite: true);

            logger.LogInfo($"Database restored successfully from: {backupFilePath}");
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            logger.LogError($"Failed to restore database from backup: {ex.Message}", ex);
            throw;
        }
    }

    public Task PruneOldBackupsAsync(int retentionDays = 7, CancellationToken cancellationToken = default)
    {
        if (retentionDays < 1) retentionDays = 7;
        Directory.CreateDirectory(folders.Backups);

        try
        {
            var cutoff = DateTime.Now.AddDays(-retentionDays);
            var dirInfo = new DirectoryInfo(folders.Backups);
            var oldFiles = dirInfo.GetFiles("*.db").Where(f => f.CreationTime < cutoff);

            foreach (var file in oldFiles)
            {
                try
                {
                    file.Delete();
                    logger.LogInfo($"Pruned old backup file: {file.Name}");
                }
                catch (Exception ex)
                {
                    logger.LogWarning($"Could not delete old backup {file.Name}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning($"Backup prune operation encountered an error: {ex.Message}");
        }

        return Task.CompletedTask;
    }

    private static string FormatFileSize(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
        return $"{bytes / (1024.0 * 1024.0):F2} MB";
    }
}
