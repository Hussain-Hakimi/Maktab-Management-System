using Microsoft.Data.Sqlite;
using Maktab.Application.Abstractions;
using Maktab.Domain.Entities;
using Maktab.Domain.Enums;

namespace Maktab.Infrastructure.Persistence;

public sealed class SqliteAttendanceRepository(
    IConnectionStringProvider connectionStringProvider) : IAttendanceRepository
{
    public async Task<IReadOnlyList<AttendanceRecord>> GetByClassAndDateAsync(
        int classId,
        DateTime date,
        CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT a.AttendanceID, a.StudentID, a.AttendanceDate, a.Status
FROM tbl_Attendance a
INNER JOIN tbl_Students s ON a.StudentID = s.StudentID
WHERE s.ClassID = $classId AND a.AttendanceDate = $date;";

        await using var connection = new SqliteConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("$classId", classId);
        command.Parameters.AddWithValue("$date", date.ToString("yyyy-MM-dd"));

        var result = new List<AttendanceRecord>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new AttendanceRecord
            {
                AttendanceId = reader.GetInt32(0),
                StudentId = reader.GetInt32(1),
                Date = DateTime.Parse(reader.GetString(2)),
                Status = ParseStatus(reader.GetString(3))
            });
        }

        return result;
    }

    public async Task<IReadOnlyList<AttendanceRecord>> GetByStudentAndRangeAsync(
        int studentId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT AttendanceID, StudentID, AttendanceDate, Status
FROM tbl_Attendance
WHERE StudentID = $studentId
  AND AttendanceDate BETWEEN $startDate AND $endDate
ORDER BY AttendanceDate;";

        await using var connection = new SqliteConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("$studentId", studentId);
        command.Parameters.AddWithValue("$startDate", startDate.ToString("yyyy-MM-dd"));
        command.Parameters.AddWithValue("$endDate", endDate.ToString("yyyy-MM-dd"));

        var result = new List<AttendanceRecord>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new AttendanceRecord
            {
                AttendanceId = reader.GetInt32(0),
                StudentId = reader.GetInt32(1),
                Date = DateTime.Parse(reader.GetString(2)),
                Status = ParseStatus(reader.GetString(3))
            });
        }

        return result;
    }

    public async Task SaveOrUpdateBatchAsync(
        IEnumerable<AttendanceRecord> records,
        CancellationToken cancellationToken = default)
    {
        const string upsertSql = @"
INSERT INTO tbl_Attendance (StudentID, AttendanceDate, Status)
VALUES ($studentId, $date, $status)
ON CONFLICT(StudentID, AttendanceDate) DO UPDATE SET
    Status = excluded.Status;";

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
                command.Parameters.AddWithValue("$date", record.Date.ToString("yyyy-MM-dd"));
                command.Parameters.AddWithValue("$status", record.Status.ToString());

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

    public async Task<int> GetAbsenceDaysByStudentAndRangeAsync(
        int studentId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT COUNT(*)
FROM tbl_Attendance
WHERE StudentID = $studentId
  AND Status = 'Absent'
  AND AttendanceDate BETWEEN $startDate AND $endDate;";

        await using var connection = new SqliteConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("$studentId", studentId);
        command.Parameters.AddWithValue("$startDate", startDate.ToString("yyyy-MM-dd"));
        command.Parameters.AddWithValue("$endDate", endDate.ToString("yyyy-MM-dd"));

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result);
    }

    private static AttendanceStatus ParseStatus(string status) => status switch
    {
        "Present" => AttendanceStatus.Present,
        "Absent" => AttendanceStatus.Absent,
        "Ill" => AttendanceStatus.Ill,
        "Permission" => AttendanceStatus.Permission,
        _ => throw new InvalidOperationException($"Unknown attendance status: {status}")
    };
}
