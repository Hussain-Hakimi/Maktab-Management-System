using Microsoft.Data.Sqlite;
using Maktab.Infrastructure.Persistence;

namespace Maktab.Tests;

public class DatabaseMigrationTests : IDisposable
{
    private const int LatestSchemaVersion = 14;

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
            Reports: Path.Combine(_tempDir, "Reports"),
            Logos: Path.Combine(_tempDir, "Logos"));

        DirectoryBootstrapper.EnsureFoldersExist(_folders);
        _connectionStringProvider = new ConnectionStringProvider(_folders);
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        try { Directory.Delete(_tempDir, recursive: true); } catch { }
    }

    [Fact]
    public async Task InitializeAsync_SetsLatestVersionAndCreatesReleaseCriticalSchema()
    {
        var initializer = new SqliteDatabaseInitializer(_connectionStringProvider);
        await initializer.InitializeAsync();

        await using var connection = new SqliteConnection(_connectionStringProvider.GetConnectionString());
        await connection.OpenAsync();

        Assert.Equal(LatestSchemaVersion, await GetUserVersionAsync(connection));

        var tables = new[]
        {
            "tbl_Classes",
            "tbl_Subjects",
            "tbl_Students",
            "tbl_ExamMarks",
            "tbl_Attendance",
            "tbl_Users",
            "tbl_Settings",
            "tbl_AcademicYears",
            "tbl_StudentPromotionHistory",
            "tbl_TeacherSubjects",
            "tbl_ClassGuardians",
            "tbl_Exams",
            "tbl_ClassFinalizations",
            "tbl_StudentAcademicEnrollments"
        };

        foreach (var table in tables)
        {
            Assert.True(await SchemaObjectExistsAsync(connection, "table", table), $"Missing table: {table}");
        }

        Assert.True(await ColumnExistsAsync(connection, "tbl_Students", "AdmissionNumber"));
        Assert.True(await SchemaObjectExistsAsync(connection, "index", "ux_students_admission_number"));
        Assert.False(await SchemaObjectExistsAsync(connection, "table", "tbl_Books"));
        Assert.False(await SchemaObjectExistsAsync(connection, "table", "tbl_BookIssues"));
        Assert.False(await SchemaObjectExistsAsync(connection, "table", "tbl_Textbooks"));
        Assert.False(await SchemaObjectExistsAsync(connection, "table", "tbl_TextbookIssues"));
        Assert.False(await SchemaObjectExistsAsync(connection, "table", "tbl_Fees"));
        Assert.False(await SchemaObjectExistsAsync(connection, "table", "tbl_FeePayments"));
        Assert.True(await SchemaObjectExistsAsync(connection, "index", "ux_class_guardians_teacher"));
    }

    [Fact]
    public async Task InitializeAsync_WhenCalledTwice_RemainsIdempotent()
    {
        var initializer = new SqliteDatabaseInitializer(_connectionStringProvider);
        await initializer.InitializeAsync();
        await initializer.InitializeAsync();

        await using var connection = new SqliteConnection(_connectionStringProvider.GetConnectionString());
        await connection.OpenAsync();

        Assert.Equal(LatestSchemaVersion, await GetUserVersionAsync(connection));

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='tbl_Students';";
        var count = Convert.ToInt32(await cmd.ExecuteScalarAsync());
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task InitializeAsync_EnforcesOneGuardianClassPerTeacher()
    {
        var initializer = new SqliteDatabaseInitializer(_connectionStringProvider);
        await initializer.InitializeAsync();

        await using var connection = new SqliteConnection(_connectionStringProvider.GetConnectionString());
        await connection.OpenAsync();
        await ExecuteNonQueryAsync(connection, @"
INSERT INTO tbl_Users (Username, PasswordHash, FullName, Role, IsActive)
VALUES ('guardian-test', 'hash', 'Guardian Test', 'Teacher', 1);
INSERT INTO tbl_Classes (GradeName, NumberOfSubjects) VALUES ('Guardian Class 1', 0);
INSERT INTO tbl_Classes (GradeName, NumberOfSubjects) VALUES ('Guardian Class 2', 0);
INSERT INTO tbl_ClassGuardians (TeacherUserID, ClassID)
VALUES ((SELECT UserID FROM tbl_Users WHERE Username = 'guardian-test'),
    (SELECT ClassID FROM tbl_Classes WHERE GradeName = 'Guardian Class 1'));
");

        await using var duplicateCommand = connection.CreateCommand();
        duplicateCommand.CommandText = @"
INSERT INTO tbl_ClassGuardians (TeacherUserID, ClassID)
VALUES ((SELECT UserID FROM tbl_Users WHERE Username = 'guardian-test'),
        (SELECT ClassID FROM tbl_Classes WHERE GradeName = 'Guardian Class 2'));";

        await Assert.ThrowsAsync<SqliteException>(() => duplicateCommand.ExecuteNonQueryAsync());
    }


    private static async Task<int> GetUserVersionAsync(SqliteConnection connection)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "PRAGMA user_version;";
        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }

    private static async Task<bool> SchemaObjectExistsAsync(SqliteConnection connection, string type, string name)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = $type AND name = $name;";
        cmd.Parameters.AddWithValue("$type", type);
        cmd.Parameters.AddWithValue("$name", name);
        return Convert.ToInt32(await cmd.ExecuteScalarAsync()) == 1;
    }

    private static async Task<bool> ColumnExistsAsync(SqliteConnection connection, string table, string column)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = $"PRAGMA table_info({table});";
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            if (string.Equals(reader.GetString(1), column, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static async Task ExecuteNonQueryAsync(SqliteConnection connection, string sql)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        await cmd.ExecuteNonQueryAsync();
    }
}
