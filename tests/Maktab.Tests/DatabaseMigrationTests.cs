using Microsoft.Data.Sqlite;
using Maktab.Infrastructure.Persistence;

namespace Maktab.Tests;

public class DatabaseMigrationTests : IDisposable
{
    private const int LatestSchemaVersion = 13;

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
            "tbl_Books",
            "tbl_BookIssues",
            "tbl_Textbooks",
            "tbl_TextbookIssues",
            "tbl_Fees",
            "tbl_FeePayments",
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
        Assert.True(await SchemaObjectExistsAsync(connection, "index", "ux_fee_payments_receipt_number"));
        Assert.True(await SchemaObjectExistsAsync(connection, "index", "idx_fees_academic_year"));
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
    public async Task InitializeAsync_FromVersion12WithThreeDuplicateReceipts_NormalizesAllAndCreatesUniqueIndex()
    {
        var initializer = new SqliteDatabaseInitializer(_connectionStringProvider);
        await initializer.InitializeAsync();

        await using (var connection = new SqliteConnection(_connectionStringProvider.GetConnectionString()))
        {
            await connection.OpenAsync();

            await ExecuteNonQueryAsync(connection, "DROP INDEX ux_fee_payments_receipt_number;");
            await SeedThreeDuplicateReceiptsAsync(connection);
            await ExecuteNonQueryAsync(connection, "PRAGMA user_version = 12;");
        }

        await initializer.InitializeAsync();

        await using var verificationConnection = new SqliteConnection(_connectionStringProvider.GetConnectionString());
        await verificationConnection.OpenAsync();

        Assert.Equal(LatestSchemaVersion, await GetUserVersionAsync(verificationConnection));
        Assert.True(await SchemaObjectExistsAsync(verificationConnection, "index", "ux_fee_payments_receipt_number"));

        await using var cmd = verificationConnection.CreateCommand();
        cmd.CommandText = @"
SELECT COUNT(*), COUNT(DISTINCT ReceiptNumber)
FROM tbl_FeePayments
WHERE ReceiptNumber LIKE 'LEGACY-DUPLICATE%';";

        await using var reader = await cmd.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        Assert.Equal(3, reader.GetInt32(0));
        Assert.Equal(3, reader.GetInt32(1));
    }

    private static async Task SeedThreeDuplicateReceiptsAsync(SqliteConnection connection)
    {
        await ExecuteNonQueryAsync(connection, @"
INSERT INTO tbl_Classes (GradeName, NumberOfSubjects) VALUES ('Migration Test Class', 0);
INSERT INTO tbl_Students (FirstName, LastName, FatherName, ClassID, RollNumber, RegistrationDate, AdmissionNumber)
VALUES ('Test', 'Student', 'Father', last_insert_rowid(), '1', '2026-01-01', 'ADM-MIGRATION-TEST');
INSERT INTO tbl_Fees (StudentID, FeeType, Amount, DueDate, CreatedDate, AcademicYearId)
VALUES (last_insert_rowid(), 'Tuition', 300, '2026-12-31', '2026-01-01', 0);
");

        await using var idsCommand = connection.CreateCommand();
        idsCommand.CommandText = @"
SELECT f.FeeID, f.StudentID
FROM tbl_Fees f
WHERE f.FeeType = 'Tuition'
ORDER BY f.FeeID DESC
LIMIT 1;";

        await using var reader = await idsCommand.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        var feeId = reader.GetInt32(0);
        var studentId = reader.GetInt32(1);
        await reader.DisposeAsync();

        for (var i = 0; i < 3; i++)
        {
            await using var paymentCommand = connection.CreateCommand();
            paymentCommand.CommandText = @"
INSERT INTO tbl_FeePayments (FeeID, StudentID, Amount, PaymentDate, ReceiptNumber)
VALUES ($feeId, $studentId, 50, '2026-01-02', 'LEGACY-DUPLICATE');";
            paymentCommand.Parameters.AddWithValue("$feeId", feeId);
            paymentCommand.Parameters.AddWithValue("$studentId", studentId);
            await paymentCommand.ExecuteNonQueryAsync();
        }
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
