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

                // Never report a backup as successful until SQLite confirms the
                // copied database is internally consistent.
                await VerifyDatabaseIntegrityAsync(destConnection, cancellationToken);
            }

            logger.LogInfo($"Backup created and verified successfully at: {backupFilePath}");

            // Auto prune old backups
            await PruneOldBackupsAsync(7, cancellationToken);

            return backupFilePath;
        }
        catch (Exception ex)
        {
            // Do not leave a backup file that failed validation and could later
            // be selected for restore as if it were a valid backup.
            try
            {
                if (File.Exists(backupFilePath))
                    File.Delete(backupFilePath);
            }
            catch (Exception cleanupEx)
            {
                logger.LogWarning($"Could not remove invalid backup {backupFilePath}: {cleanupEx.Message}");
            }

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

        var mainDbPath = Path.Combine(folders.Data, "maktab.db");
        var mainDbWal = Path.Combine(folders.Data, "maktab.db-wal");
        var mainDbShm = Path.Combine(folders.Data, "maktab.db-shm");
        var safetyBackupPath = Path.Combine(
            folders.Backups,
            $"maktab_pre_restore_{DateTime.Now:yyyyMMdd_HHmmss_fff}.db");

        try
        {
            // Validate the selected backup before making any destructive change.
            await VerifyDatabaseFileIntegrityAsync(backupFilePath, cancellationToken);

            SqliteConnection.ClearAllPools();

            // Preserve the currently running database so a failed restore never
            // destroys the only known-good copy.
            if (File.Exists(mainDbPath))
            {
                Directory.CreateDirectory(folders.Backups);
                File.Copy(mainDbPath, safetyBackupPath, overwrite: false);
                logger.LogInfo($"Pre-restore safety backup created at: {safetyBackupPath}");
            }

            if (File.Exists(mainDbWal)) File.Delete(mainDbWal);
            if (File.Exists(mainDbShm)) File.Delete(mainDbShm);

            File.Copy(backupFilePath, mainDbPath, overwrite: true);

            // Validate the database after replacement as an additional guard.
            await VerifyDatabaseFileIntegrityAsync(mainDbPath, cancellationToken);

            logger.LogInfo($"Database restored and verified successfully from: {backupFilePath}");
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

    private static async Task VerifyDatabaseFileIntegrityAsync(
        string databasePath,
        CancellationToken cancellationToken)
    {
        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadOnly
        };

        await using var connection = new SqliteConnection(builder.ToString());
        await connection.OpenAsync(cancellationToken);
        await VerifyDatabaseIntegrityAsync(connection, cancellationToken);
    }

    private static async Task VerifyDatabaseIntegrityAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA integrity_check;";
        var result = Convert.ToString(await command.ExecuteScalarAsync(cancellationToken));

        if (!string.Equals(result, "ok", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException(
                $"SQLite database integrity check failed. SQLite reported: {result}");
        }
    }

    private static string FormatFileSize(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
        return $"{bytes / (1024.0 * 1024.0):F2} MB";
    }

    public Task<DateTime?> GetLastBackupDateAsync(CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(folders.Backups))
            return Task.FromResult<DateTime?>(null);

        var files = Directory.GetFiles(folders.Backups, "*.db");
        if (files.Length == 0)
            return Task.FromResult<DateTime?>(null);

        var latest = files.Max(f => File.GetCreationTime(f));
        return Task.FromResult<DateTime?>(latest);
    }

    public Task<IReadOnlyList<string>> GetRemovableDrivePathsAsync()
    {
        return Task.FromResult<IReadOnlyList<string>>(
            DriveInfo.GetDrives()
                .Where(d => d.DriveType == DriveType.Removable && d.IsReady)
                .Select(d => d.RootDirectory.FullName)
                .ToList());
    }
}
