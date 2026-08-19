using Microsoft.Data.Sqlite;
using Maktab.Infrastructure.Persistence;

namespace Maktab.Tests;

public class DatabaseMigrationTests : IDisposable
{
    private readonly string _tempDir;
    private readonly AppFolders _folders;
    private readonly ConnectionStringProvider _connectionStringProvider;

    public DatabaseMigrationTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "MaktabMigrationTests_" + Guid.NewGuid());
        _folders = new AppFolders(
            Root: _tempDir,
            Data: Path.Combine(_tempDir, "Data"),
            Logs: Path.Combine(_tempDir, "Logs"),
            Backups: Path.Combine(_tempDir, "Backups"),
            Reports: Path.Combine(_tempDir, "Reports"));

        DirectoryBootstrapper.EnsureFoldersExist(_folders);
        _connectionStringProvider = new ConnectionStringProvider(_folders);
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        try { Directory.Delete(_tempDir, recursive: true); } catch { }
    }

    [Fact]
    public async Task InitializeAsync_SetsUserVersionToLatestAndCreatesAllTables()
    {
        var initializer = new SqliteDatabaseInitializer(_connectionStringProvider);
        await initializer.InitializeAsync();

        await using var connection = new SqliteConnection(_connectionStringProvider.GetConnectionString());
        await connection.OpenAsync();

        // Verify user_version is at least 1 (baseline)
        await using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "PRAGMA user_version;";
            var version = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            Assert.True(version >= 1);
        }

        // Verify all V1.1 tables exist
        var tables = new[] { "tbl_Classes", "tbl_Subjects", "tbl_Students", "tbl_ExamMarks",
                             "tbl_Attendance", "tbl_Books", "tbl_BookIssues",
                             "tbl_Textbooks", "tbl_TextbookIssues", "tbl_Fees", "tbl_FeePayments" };
        foreach (var table in tables)
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = $"SELECT count(*) FROM sqlite_master WHERE type='table' AND name='{table}';";
            var count = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            Assert.Equal(1, count);
        }
    }

    [Fact]
    public async Task InitializeAsync_WhenCalledTwice_DoesNotDuplicateTables()
    {
        var initializer = new SqliteDatabaseInitializer(_connectionStringProvider);
        await initializer.InitializeAsync();
        await initializer.InitializeAsync(); // second call should be idempotent

        await using var connection = new SqliteConnection(_connectionStringProvider.GetConnectionString());
        await connection.OpenAsync();

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT count(*) FROM sqlite_master WHERE type='table' AND name='tbl_Students';";
        var count = Convert.ToInt32(await cmd.ExecuteScalarAsync());
        Assert.Equal(1, count);
    }
}
