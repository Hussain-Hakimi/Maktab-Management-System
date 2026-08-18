using Microsoft.Data.Sqlite;
using Maktab.Application.Abstractions;
using Maktab.Domain.Entities;
using Maktab.Domain.Enums;

namespace Maktab.Infrastructure.Persistence;

public sealed class SqliteAttendanceRepository(IConnectionStringProvider connectionStringProvider) : IAttendanceRepository
{
    public async Task<IReadOnlyList<AttendanceRecord>> GetByClassAndDateAsync(
        int classId,
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT a.AttendanceID, a.StudentID, a.AttendanceDate, a.Status, a.Notes
FROM tbl_Attendance a
INNER JOIN tbl_Students s ON a.StudentID = s.StudentID
WHERE s.ClassID = $classId AND a.AttendanceDate = $date
ORDER BY s.RollNumber;";

        await using var connection = new SqliteConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("$classId", classId);
        command.Parameters.AddWithValue("$date", FormatDate(date));

        return await ReadRecordsAsync(command, cancellationToken);
    }

    public async Task<IReadOnlyList<AttendanceRecord>> GetByStudentAsync(
        int studentId,
        CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT AttendanceID, StudentID, AttendanceDate, Status, Notes
FROM tbl_Attendance
WHERE StudentID = $studentId
ORDER BY AttendanceDate;";

        await using var connection = new SqliteConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("$studentId", studentId);

        return await ReadRecordsAsync(command, cancellationToken);
    }

    public async Task<IReadOnlyList<AttendanceRecord>> GetByClassAndDateRangeAsync(
        int classId,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT a.AttendanceID, a.StudentID, a.AttendanceDate, a.Status, a.Notes
FROM tbl_Attendance a
INNER JOIN tbl_Students s ON a.StudentID = s.StudentID
WHERE s.ClassID = $classId AND a.AttendanceDate BETWEEN $fromDate AND $toDate
ORDER BY a.AttendanceDate, s.RollNumber;";

        await using var connection = new SqliteConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("$classId", classId);
        command.Parameters.AddWithValue("$fromDate", FormatDate(fromDate));
        command.Parameters.AddWithValue("$toDate", FormatDate(toDate));

        return await ReadRecordsAsync(command, cancellationToken);
    }

    public async Task SaveBatchAsync(
        IEnumerable<AttendanceRecord> records,
        CancellationToken cancellationToken = default)
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
                command.Parameters.AddWithValue("$date", FormatDate(record.Date));
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

    public async Task<int> GetAbsenceCountAsync(int studentId, CancellationToken cancellationToken = default)
    {
        // Only unexcused "Absent" days count toward the promotion rule limit.
        const string sql = @"
SELECT COUNT(1)
FROM tbl_Attendance
WHERE StudentID = $studentId AND Status = 'Absent';";

        await using var connection = new SqliteConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("$studentId", studentId);

        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
    }

    private static string FormatDate(DateOnly date) => date.ToString("yyyy-MM-dd");

    private static async Task<IReadOnlyList<AttendanceRecord>> ReadRecordsAsync(
        SqliteCommand command,
        CancellationToken cancellationToken)
    {
        var result = new List<AttendanceRecord>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new AttendanceRecord
            {
                AttendanceId = reader.GetInt32(0),
                StudentId = reader.GetInt32(1),
                Date = DateOnly.ParseExact(reader.GetString(2), "yyyy-MM-dd"),
                Status = Enum.Parse<AttendanceStatus>(reader.GetString(3)),
                Notes = reader.IsDBNull(4) ? null : reader.GetString(4)
            });
        }

        return result;
    }
}
