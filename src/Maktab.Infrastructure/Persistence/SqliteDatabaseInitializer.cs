using Microsoft.Data.Sqlite;

namespace Maktab.Infrastructure.Persistence;

public sealed class SqliteDatabaseInitializer(IConnectionStringProvider connectionStringProvider) : IDatabaseInitializer
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);

        // Pragmas must be set on every connection; initializer runs once at startup.
        var pragmas = @"
PRAGMA foreign_keys = ON;
PRAGMA journal_mode = WAL;
PRAGMA synchronous = NORMAL;
";
        await ExecuteNonQueryAsync(connection, pragmas, cancellationToken);

        // Run database migrations
        await RunMigrationsAsync(connection, cancellationToken);
    }

    private static async Task RunMigrationsAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        // 1. Get current user_version
        int currentVersion = await GetUserVersionAsync(connection, cancellationToken);

        // 2. If database is new (version 0), apply baseline schema and set version 1
        if (currentVersion == 0)
        {
            await ExecuteNonQueryAsync(connection, DatabaseMigrations.BaselineSql, cancellationToken);
            await SetUserVersionAsync(connection, 1, cancellationToken);
            currentVersion = 1;
        }

        // 3. Apply any pending migrations beyond the baseline
        var migrations = DatabaseMigrations.GetMigrations()
            .Where(m => m.Version > currentVersion)
            .OrderBy(m => m.Version);

        foreach (var migration in migrations)
        {
            await ExecuteNonQueryAsync(connection, migration.Sql, cancellationToken);
            await SetUserVersionAsync(connection, migration.Version, cancellationToken);
        }
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
