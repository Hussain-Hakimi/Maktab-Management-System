using Microsoft.Data.Sqlite;
using Maktab.Application.Abstractions;
using Maktab.Domain.Entities;
using Maktab.Domain.Enums;

namespace Maktab.Infrastructure.Persistence;

public sealed class SqliteAttendanceRepository(IConnectionStringProvider connectionStringProvider) : IAttendanceRepository
{
    private const string DateFormat = "yyyy-MM-dd";

    public async Task<IReadOnlyList<AttendanceRecord>> GetByClassAndDateAsync(int classId, DateOnly date, CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT a.StudentID, a.AttendanceDate, a.Status, a.Notes
FROM tbl_Attendance a
INNER JOIN tbl_Students s ON a.StudentID = s.StudentID
WHERE s.ClassID = $classId AND a.AttendanceDate = $date;";

        await using var connection = new SqliteConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("$classId", classId);
        command.Parameters.AddWithValue("$date", date.ToString(DateFormat));

        var result = new List<AttendanceRecord>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(MapRecord(reader));
        }

        return result;
    }

    public async Task<IReadOnlyList<AttendanceRecord>> GetByStudentAsync(int studentId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT StudentID, AttendanceDate, Status, Notes
FROM tbl_Attendance
WHERE StudentID = $studentId
ORDER BY AttendanceDate;";

        await using var connection = new SqliteConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("$studentId", studentId);

        var result = new List<AttendanceRecord>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(MapRecord(reader));
        }

        return result;
    }

    public async Task<IReadOnlyList<AttendanceRecord>> GetByClassAndDateRangeAsync(int classId, DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT a.StudentID, a.AttendanceDate, a.Status, a.Notes
FROM tbl_Attendance a
INNER JOIN tbl_Students s ON a.StudentID = s.StudentID
WHERE s.ClassID = $classId AND a.AttendanceDate >= $fromDate AND a.AttendanceDate <= $toDate
ORDER BY a.AttendanceDate;";

        await using var connection = new SqliteConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("$classId", classId);
        command.Parameters.AddWithValue("$fromDate", fromDate.ToString(DateFormat));
        command.Parameters.AddWithValue("$toDate", toDate.ToString(DateFormat));

        var result = new List<AttendanceRecord>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(MapRecord(reader));
        }

        return result;
    }

    public async Task<int> GetStatusCountAsync(int studentId, AttendanceStatus status, CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT COUNT(1)
FROM tbl_Attendance
WHERE StudentID = $studentId AND Status = $status;";

        await using var connection = new SqliteConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("$studentId", studentId);
        command.Parameters.AddWithValue("$status", status.ToString());

        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
    }

    public async Task SaveBatchAsync(IEnumerable<AttendanceRecord> records, CancellationToken cancellationToken = default)
    {
        const string upsertSql = @"
INSERT INTO tbl_Attendance (StudentID, AttendanceDate, Status, Notes)
VALUES ($studentId, $date, $status, $notes)
ON CONFLICT(StudentID, AttendanceDate) DO UPDATE SET
    Status = excluded.Status,
    Notes = excluded.Notes;";

        await using var connection = new SqliteConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            foreach (var record in records)
            {
                await using var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = upsertSql;
                command.Parameters.AddWithValue("$studentId", record.StudentId);
                command.Parameters.AddWithValue("$date", record.Date.ToString(DateFormat));
                command.Parameters.AddWithValue("$status", record.Status.ToString());
                command.Parameters.AddWithValue("$notes", (object?)record.Notes ?? DBNull.Value);

                await command.ExecuteNonQueryAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static AttendanceRecord MapRecord(SqliteDataReader reader)
    {
        return new AttendanceRecord
        {
            StudentId = reader.GetInt32(0),
            Date = DateOnly.ParseExact(reader.GetString(1), DateFormat),
            Status = Enum.Parse<AttendanceStatus>(reader.GetString(2)),
            Notes = reader.IsDBNull(3) ? null : reader.GetString(3)
        };
    }
}
