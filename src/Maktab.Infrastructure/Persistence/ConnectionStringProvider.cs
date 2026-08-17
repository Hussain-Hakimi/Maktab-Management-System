using Microsoft.Data.Sqlite;

namespace Maktab.Infrastructure.Persistence;

public sealed class ConnectionStringProvider(AppFolders folders) : IConnectionStringProvider
{
    public string GetConnectionString()
    {
        var databasePath = Path.Combine(folders.Data, "maktab.db");
        // ForeignKeys = true enforces FK constraints (CASCADE/RESTRICT) on EVERY connection.
        // PRAGMA foreign_keys in the initializer only applied to that single connection.
        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared,
            ForeignKeys = true
        };

        return builder.ToString();
    }
}
