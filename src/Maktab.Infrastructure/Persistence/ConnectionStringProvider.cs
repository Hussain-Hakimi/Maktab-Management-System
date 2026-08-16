using Microsoft.Data.Sqlite;

namespace Maktab.Infrastructure.Persistence;

public sealed class ConnectionStringProvider(AppFolders folders) : IConnectionStringProvider
{
    public string GetConnectionString()
    {
        var databasePath = Path.Combine(folders.Data, "maktab.db");
        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared
        };

        return builder.ToString();
    }
}