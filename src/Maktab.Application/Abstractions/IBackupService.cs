namespace Maktab.Application.Abstractions;

public interface IBackupService
{
    Task<string> CreateBackupAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BackupInfoDto>> GetBackupsListAsync(CancellationToken cancellationToken = default);
    Task RestoreBackupAsync(string backupFilePath, CancellationToken cancellationToken = default);
    Task PruneOldBackupsAsync(int retentionDays = 7, CancellationToken cancellationToken = default);
}
