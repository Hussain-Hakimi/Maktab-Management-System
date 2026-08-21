using Microsoft.Data.Sqlite;
using Maktab.Application.Abstractions;
using Maktab.Domain.Entities;

namespace Maktab.Infrastructure.Persistence;

public sealed class SqliteSettingRepository(IConnectionStringProvider connectionStringProvider) : ISettingRepository
{
    public async Task<Setting?> GetByKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT SettingID, Key, Value FROM tbl_Settings WHERE Key = $key;";

        await using var connection = new SqliteConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("$key", key);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return new Setting
            {
                SettingId = reader.GetInt32(0),
                Key = reader.GetString(1),
                Value = reader.GetString(2)
            };
        }

        return null;
    }

    public async Task<IReadOnlyList<Setting>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT SettingID, Key, Value FROM tbl_Settings;";

        await using var connection = new SqliteConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        var settings = new List<Setting>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            settings.Add(new Setting
            {
                SettingId = reader.GetInt32(0),
                Key = reader.GetString(1),
                Value = reader.GetString(2)
            });
        }

        return settings;
    }

    public async Task UpsertAsync(Setting setting, CancellationToken cancellationToken = default)
    {
        const string sql = @"
INSERT INTO tbl_Settings (Key, Value)
VALUES ($key, $value)
ON CONFLICT(Key) DO UPDATE SET Value = excluded.Value;";

        await using var connection = new SqliteConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = sql;
            command.Parameters.AddWithValue("$key", setting.Key);
            command.Parameters.AddWithValue("$value", setting.Value);

            await command.ExecuteNonQueryAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
