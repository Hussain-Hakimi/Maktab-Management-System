using Microsoft.Data.Sqlite;
using Maktab.Application.Services;

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
        // Check if any user exists
        await using (var checkCmd = connection.CreateCommand())
        {
            checkCmd.CommandText = "SELECT COUNT(1) FROM tbl_Users;";
            var count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync(cancellationToken));
            if (count > 0)
                return;
        }

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
