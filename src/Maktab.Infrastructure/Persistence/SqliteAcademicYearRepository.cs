using Microsoft.Data.Sqlite;
using Maktab.Application.Abstractions;
using Maktab.Domain.Entities;

namespace Maktab.Infrastructure.Persistence;

public sealed class SqliteAcademicYearRepository(IConnectionStringProvider connectionStringProvider) : IAcademicYearRepository
{
    public async Task<AcademicYear?> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT AcademicYearID, YearName, StartDate, EndDate, IsActive FROM tbl_AcademicYears WHERE IsActive = 1 LIMIT 1;";

        await using var connection = new SqliteConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
            return MapYear(reader);

        return null;
    }

    public async Task<AcademicYear?> GetByIdAsync(int academicYearId, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT AcademicYearID, YearName, StartDate, EndDate, IsActive FROM tbl_AcademicYears WHERE AcademicYearID = $id;";

        await using var connection = new SqliteConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("$id", academicYearId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
            return MapYear(reader);

        return null;
    }

    public async Task<IReadOnlyList<AcademicYear>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT AcademicYearID, YearName, StartDate, EndDate, IsActive FROM tbl_AcademicYears ORDER BY StartDate;";

        await using var connection = new SqliteConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        var years = new List<AcademicYear>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            years.Add(MapYear(reader));
        }

        return years;
    }

    public async Task<int> CreateAsync(AcademicYear academicYear, CancellationToken cancellationToken = default)
    {
        const string sql = @"
INSERT INTO tbl_AcademicYears (YearName, StartDate, EndDate, IsActive)
VALUES ($name, $start, $end, $isActive);
SELECT last_insert_rowid();";

        await using var connection = new SqliteConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = sql;
            command.Parameters.AddWithValue("$name", academicYear.YearName);
            command.Parameters.AddWithValue("$start", academicYear.StartDate.ToString("yyyy-MM-dd"));
            command.Parameters.AddWithValue("$end", academicYear.EndDate.ToString("yyyy-MM-dd"));
            command.Parameters.AddWithValue("$isActive", academicYear.IsActive ? 1 : 0);

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

    public async Task SetActiveAsync(int academicYearId, CancellationToken cancellationToken = default)
    {
        const string clearActiveSql = "UPDATE tbl_AcademicYears SET IsActive = 0 WHERE IsActive = 1;";
        const string setActiveSql = "UPDATE tbl_AcademicYears SET IsActive = 1 WHERE AcademicYearID = $id;";

        await using var connection = new SqliteConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            await using (var clearCmd = connection.CreateCommand())
            {
                clearCmd.Transaction = transaction;
                clearCmd.CommandText = clearActiveSql;
                await clearCmd.ExecuteNonQueryAsync(cancellationToken);
            }

            await using (var setCmd = connection.CreateCommand())
            {
                setCmd.Transaction = transaction;
                setCmd.CommandText = setActiveSql;
                setCmd.Parameters.AddWithValue("$id", academicYearId);
                var affected = await setCmd.ExecuteNonQueryAsync(cancellationToken);
                if (affected == 0)
                    throw new InvalidOperationException("Academic year not found.");
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static AcademicYear MapYear(SqliteDataReader reader) => new()
    {
        AcademicYearId = reader.GetInt32(0),
        YearName = reader.GetString(1),
        StartDate = DateTime.Parse(reader.GetString(2)),
        EndDate = DateTime.Parse(reader.GetString(3)),
        IsActive = reader.GetInt32(4) == 1
    };
}
