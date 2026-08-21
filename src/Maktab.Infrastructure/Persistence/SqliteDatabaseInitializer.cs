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
        // Insert default promotion settings if not present
        const string upsertSql = @"
INSERT INTO tbl_Settings (Key, Value)
VALUES ($key, $value)
ON CONFLICT(Key) DO NOTHING;";

        await using var checkCmd = connection.CreateCommand();
        checkCmd.CommandText = "SELECT COUNT(1) FROM tbl_Settings WHERE Key = 'Promotion.PassingAverage';";
        var existing = Convert.ToInt32(await checkCmd.ExecuteScalarAsync(cancellationToken));
        if (existing == 0)
        {
            await ExecuteUpsertAsync(connection, "Promotion.PassingAverage", "65", cancellationToken);
            await ExecuteUpsertAsync(connection, "Promotion.PassingMark", "40", cancellationToken);
            await ExecuteUpsertAsync(connection, "Promotion.MaxFailedSubjects", "3", cancellationToken);
            await ExecuteUpsertAsync(connection, "Promotion.MaxAbsenceDays", "30", cancellationToken);
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

    private static async Task ExecuteUpsertAsync(
        SqliteConnection connection,
        string key,
        string value,
        CancellationToken cancellationToken)
    {
        const string sql = "INSERT INTO tbl_Settings (Key, Value) VALUES ($key, $value) ON CONFLICT(Key) DO NOTHING;";
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("$key", key);
        command.Parameters.AddWithValue("$value", value);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static decimal GetDecimal(Dictionary<string, string> dict, string key, decimal defaultValue)
    {
        return dict.TryGetValue(key, out var value) && decimal.TryParse(value, out var result) ? result : defaultValue;
    }

    private static int GetInt(Dictionary<string, string> dict, string key, int defaultValue)
    {
        return dict.TryGetValue(key, out var value) && int.TryParse(value, out var result) ? result : defaultValue;
    }

    private static async Task<int> GetUserVersionAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA user_version;";
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result);
    }

    private static async Task SetUserVersionAsync(
        SqliteConnection connection,
        int version,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA user_version = {version};";
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task ExecuteNonQueryAsync(
        SqliteConnection connection,
        string sql,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
