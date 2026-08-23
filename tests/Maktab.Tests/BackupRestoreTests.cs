using Microsoft.Data.Sqlite;
using Maktab.Domain.Entities;
using Maktab.Infrastructure.Logging;
using Maktab.Infrastructure.Persistence;

namespace Maktab.Tests;

/// <summary>
/// Backup/restore round-trip tests against a real (temporary) SQLite database:
/// create data, back up, change data, restore, and verify the original data is back.
/// </summary>
public class BackupRestoreTests : IDisposable
{
    private readonly string _tempDir;
    private readonly AppFolders _folders;
    private readonly ConnectionStringProvider _connectionStringProvider;
    private readonly SqliteBackupService _backupService;
    private readonly SqliteClassSubjectRepository _classSubjectRepository;

    public BackupRestoreTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "MaktabBackupRestoreTests_" + Guid.NewGuid());
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

        var logger = new FileAppLogger(_folders);
        _backupService = new SqliteBackupService(_folders, _connectionStringProvider, logger);
        _classSubjectRepository = new SqliteClassSubjectRepository(_connectionStringProvider);
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
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
    public async Task BackupThenRestore_AfterDataChange_BringsBackOriginalData()
    {
        // Arrange: original data
        var originalClassId = await _classSubjectRepository.CreateClassAsync(new SchoolClass
        {
            GradeName = "صنف هفتم",
            NumberOfSubjects = 8
        });

        // Act 1: backup
        var backupPath = await _backupService.CreateBackupAsync();
        Assert.True(File.Exists(backupPath));

        // Act 2: change data after the backup
        await _classSubjectRepository.CreateClassAsync(new SchoolClass
        {
            GradeName = "صنف هشتم",
            NumberOfSubjects = 9
        });
        var classesAfterChange = await _classSubjectRepository.GetClassesAsync();
        Assert.Equal(2, classesAfterChange.Count);

        // Act 3: restore
        await _backupService.RestoreBackupAsync(backupPath);

        // Assert: only the original data remains
        var classesAfterRestore = await _classSubjectRepository.GetClassesAsync();
        Assert.Single(classesAfterRestore);
        Assert.Equal(originalClassId, classesAfterRestore[0].ClassId);
        Assert.Equal("صنف هفتم", classesAfterRestore[0].GradeName);
    }

    [Fact]
    public async Task CreateBackup_AppearsInBackupsList()
    {
        var backupPath = await _backupService.CreateBackupAsync();

        var backups = await _backupService.GetBackupsListAsync();

        Assert.Single(backups);
        Assert.Equal(Path.GetFileName(backupPath), backups[0].FileName);
        Assert.True(backups[0].FileSizeBytes > 0);
    }

    [Fact]
    public async Task RestoreBackup_WhenFileDoesNotExist_ThrowsFileNotFoundException()
    {
        var missingPath = Path.Combine(_folders.Backups, "maktab_backup_missing.db");

        await Assert.ThrowsAsync<FileNotFoundException>(async () =>
        {
            await _backupService.RestoreBackupAsync(missingPath);
        });
    }
}
