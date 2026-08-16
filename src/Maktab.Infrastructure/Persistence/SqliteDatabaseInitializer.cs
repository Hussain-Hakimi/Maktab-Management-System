using Microsoft.Data.Sqlite;

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
        await ExecuteNonQueryAsync(connection, SchemaSql.Script, cancellationToken);
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