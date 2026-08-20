using Microsoft.Data.Sqlite;
using Maktab.Application.Abstractions;
using Maktab.Domain.Entities;

namespace Maktab.Infrastructure.Persistence;

public sealed class SqliteAuditLogRepository(IConnectionStringProvider connectionStringProvider) : IAuditLogRepository
{
    public async Task<IReadOnlyList<AuditLog>> GetRecentAsync(int maxRows, CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT LogID, UserName, Action, Timestamp
FROM tbl_AuditLog
ORDER BY Timestamp DESC
LIMIT $maxRows;";

        await using var connection = new SqliteConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("$maxRows", maxRows);

        var result = new List<AuditLog>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new AuditLog
            {
                LogId = reader.GetInt32(0),
                UserName = reader.GetString(1),
                Action = reader.GetString(2),
                Timestamp = DateTime.Parse(reader.GetString(3))
            });
        }

        return result;
    }

    public async Task<int> AddAsync(AuditLog log, CancellationToken cancellationToken = default)
    {
        const string sql = @"
INSERT INTO tbl_AuditLog (UserName, Action, Timestamp)
VALUES ($userName, $action, $timestamp);
SELECT last_insert_rowid();";

        await using var connection = new SqliteConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = sql;
            command.Parameters.AddWithValue("$userName", log.UserName);
            command.Parameters.AddWithValue("$action", log.Action);
            command.Parameters.AddWithValue("$timestamp", log.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"));

            var id = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
            await transaction.CommitAsync(cancellationToken);
            return id;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
