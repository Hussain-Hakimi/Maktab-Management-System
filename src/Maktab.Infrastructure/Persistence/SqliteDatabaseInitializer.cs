using Microsoft.Data.Sqlite;
using Maktab.Application.Services;
using Maktab.Domain.Rules;

namespace Maktab.Infrastructure.Persistence;

public sealed class SqliteDatabaseInitializer(IConnectionStringProvider connectionStringProvider) : IDatabaseInitializer
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);

        var pragmas = @"
PRAGMA foreign_keys = ON;
PRAGMA journal_mode = WAL;
PRAGMA synchronous = NORMAL;
";
        await ExecuteNonQueryAsync(connection, pragmas, cancellationToken);

        await RunMigrationsAsync(connection, cancellationToken);
        await SeedDefaultAdminAsync(connection, cancellationToken);
        await SeedDefaultSettingsAsync(connection, cancellationToken);
        await SeedDefaultSchoolSettingsAsync(connection, cancellationToken);
        await SeedDefaultAcademicYearAsync(connection, cancellationToken);
        await LoadPromotionSettingsIntoPolicyAsync(connection, cancellationToken);
    }

    private static async Task RunMigrationsAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        int currentVersion = await GetUserVersionAsync(connection, cancellationToken);

        if (currentVersion == 0)
        {
            await ExecuteNonQueryAsync(connection, DatabaseMigrations.BaselineSql, cancellationToken);
            await SetUserVersionAsync(connection, 1, cancellationToken);
            currentVersion = 1;
        }

        var migrations = DatabaseMigrations.GetMigrations()
            .Where(m => m.Version > currentVersion)
            .OrderBy(m => m.Version);

        foreach (var migration in migrations)
        {
            await ExecuteNonQueryAsync(connection, migration.Sql, cancellationToken);
            await SetUserVersionAsync(connection, migration.Version, cancellationToken);
        }
    }

    private static async Task SeedDefaultAdminAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        await using var checkCmd = connection.CreateCommand();
        checkCmd.CommandText = "SELECT COUNT(1) FROM tbl_Users;";
        var count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync(cancellationToken));
        if (count > 0)
            return;

        const string insertSql = @"
INSERT INTO tbl_Users (Username, PasswordHash, FullName, Role, IsActive)
VALUES ($username, $passwordHash, $fullName, 'Admin', 1);";

        await using var command = connection.CreateCommand();
        command.CommandText = insertSql;
        command.Parameters.AddWithValue("$username", "admin");
        command.Parameters.AddWithValue("$passwordHash", PasswordHasher.HashPassword("admin123"));
        command.Parameters.AddWithValue("$fullName", "مدیر سیستم");
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task SeedDefaultSettingsAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        await ExecuteUpsertIfNotExistsAsync(connection, "Promotion.PassingAverage", "65", cancellationToken);
        await ExecuteUpsertIfNotExistsAsync(connection, "Promotion.PassingMark", "40", cancellationToken);
        await ExecuteUpsertIfNotExistsAsync(connection, "Promotion.MaxFailedSubjects", "3", cancellationToken);
        await ExecuteUpsertIfNotExistsAsync(connection, "Promotion.MaxAbsenceDays", "30", cancellationToken);
    }

    private static async Task SeedDefaultSchoolSettingsAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        await ExecuteUpsertIfNotExistsAsync(connection, "School.Name", "مکتب نمونه", cancellationToken);
        await ExecuteUpsertIfNotExistsAsync(connection, "School.Address", "", cancellationToken);
        await ExecuteUpsertIfNotExistsAsync(connection, "School.Phone", "", cancellationToken);
        await ExecuteUpsertIfNotExistsAsync(connection, "School.AcademicYear", AcademicYearProvider.GetCurrentAcademicYear(), cancellationToken);
        await ExecuteUpsertIfNotExistsAsync(connection, "School.LogoPath", "", cancellationToken);

        // New keys for official headers
        await ExecuteUpsertIfNotExistsAsync(connection, "GovernmentTitle", "امارت اسلامی افغانستان", cancellationToken);
        await ExecuteUpsertIfNotExistsAsync(connection, "ProvincialEducationHeader", "ریاست معارف ولایت کابل", cancellationToken);
        await ExecuteUpsertIfNotExistsAsync(connection, "DistrictEducationHeader", "مدیریت معارف ولسوالی", cancellationToken);
    }

    private static async Task SeedDefaultAcademicYearAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        await using var checkCmd = connection.CreateCommand();
        checkCmd.CommandText = "SELECT COUNT(1) FROM tbl_AcademicYears;";
        var count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync(cancellationToken));
        if (count > 0) return;

        var yearName = AcademicYearProvider.GetCurrentAcademicYear();
        var (start, end) = ShamsiDateHelper.GetAcademicYearRange(yearName);

        const string insertSql = @"
INSERT INTO tbl_AcademicYears (YearName, StartDate, EndDate, IsActive)
VALUES ($name, $start, $end, 1);
SELECT last_insert_rowid();";

        await using var insertCmd = connection.CreateCommand();
        insertCmd.CommandText = insertSql;
        insertCmd.Parameters.AddWithValue("$name", yearName);
        insertCmd.Parameters.AddWithValue("$start", start.ToString("yyyy-MM-dd"));
        insertCmd.Parameters.AddWithValue("$end", end.ToString("yyyy-MM-dd"));
        var yearId = Convert.ToInt32(await insertCmd.ExecuteScalarAsync(cancellationToken));

        await using var updateMarks = connection.CreateCommand();
        updateMarks.CommandText = "UPDATE tbl_ExamMarks SET AcademicYearId = $id WHERE AcademicYearId = 0;";
        updateMarks.Parameters.AddWithValue("$id", yearId);
        await updateMarks.ExecuteNonQueryAsync(cancellationToken);

        await using var updateAttendance = connection.CreateCommand();
        updateAttendance.CommandText = "UPDATE tbl_Attendance SET AcademicYearId = $id WHERE AcademicYearId = 0;";
        updateAttendance.Parameters.AddWithValue("$id", yearId);
        await updateAttendance.ExecuteNonQueryAsync(cancellationToken);

        await using var updateFees = connection.CreateCommand();
        updateFees.CommandText = "UPDATE tbl_Fees SET AcademicYearId = $id WHERE AcademicYearId = 0;";
        updateFees.Parameters.AddWithValue("$id", yearId);
        await updateFees.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task ExecuteUpsertIfNotExistsAsync(
        SqliteConnection connection,
        string key,
        string value,
        CancellationToken cancellationToken)
    {
        await using var checkCmd = connection.CreateCommand();
        checkCmd.CommandText = "SELECT COUNT(1) FROM tbl_Settings WHERE Key = $key;";
        checkCmd.Parameters.AddWithValue("$key", key);
        var exists = Convert.ToInt32(await checkCmd.ExecuteScalarAsync(cancellationToken)) > 0;
        if (!exists)
        {
            const string insertSql = "INSERT INTO tbl_Settings (Key, Value) VALUES ($key, $value);";
            await using var insertCmd = connection.CreateCommand();
            insertCmd.CommandText = insertSql;
            insertCmd.Parameters.AddWithValue("$key", key);
            insertCmd.Parameters.AddWithValue("$value", value);
            await insertCmd.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private static async Task LoadPromotionSettingsIntoPolicyAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        var settings = new Dictionary<string, string>();

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT Key, Value FROM tbl_Settings;";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            settings[reader.GetString(0)] = reader.GetString(1);
        }

        var passingAverage = GetDecimal(settings, "Promotion.PassingAverage", 65m);
        var passingMark = GetDecimal(settings, "Promotion.PassingMark", 40m);
        var maxFailed = GetInt(settings, "Promotion.MaxFailedSubjects", 3);
        var maxAbsence = GetInt(settings, "Promotion.MaxAbsenceDays", 30);

        PromotionPolicy.SetValues(passingAverage, passingMark, maxFailed, maxAbsence);
    }

    private static decimal GetDecimal(Dictionary<string, string> dict, string key, decimal defaultValue)
        => dict.TryGetValue(key, out var value) && decimal.TryParse(value, out var result) ? result : defaultValue;

    private static int GetInt(Dictionary<string, string> dict, string key, int defaultValue)
        => dict.TryGetValue(key, out var value) && int.TryParse(value, out var result) ? result : defaultValue;

    private static async Task<int> GetUserVersionAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA user_version;";
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result);
    }

    private static async Task SetUserVersionAsync(SqliteConnection connection, int version, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA user_version = {version};";
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task ExecuteNonQueryAsync(SqliteConnection connection, string sql, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
