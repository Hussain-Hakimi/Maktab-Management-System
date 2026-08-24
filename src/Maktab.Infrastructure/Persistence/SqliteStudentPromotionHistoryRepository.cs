using Microsoft.Data.Sqlite;
using Maktab.Application.Abstractions;
using Maktab.Domain.Entities;

namespace Maktab.Infrastructure.Persistence;

public sealed class SqliteStudentPromotionHistoryRepository(IConnectionStringProvider connectionStringProvider) : IStudentPromotionHistoryRepository
{
    public async Task<int> AddAsync(StudentPromotionHistory history, CancellationToken cancellationToken = default)
    {
        const string sql = @"
INSERT INTO tbl_StudentPromotionHistory (StudentID, FromClassID, ToClassID, AcademicYearID, Result, PromotionDate)
VALUES ($studentId, $fromClassId, $toClassId, $academicYearId, $result, $promotionDate);
SELECT last_insert_rowid();";

        await using var connection = new SqliteConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = sql;
            command.Parameters.AddWithValue("$studentId", history.StudentId);
            command.Parameters.AddWithValue("$fromClassId", history.FromClassId);
            command.Parameters.AddWithValue("$toClassId", (object?)history.ToClassId ?? DBNull.Value);
            command.Parameters.AddWithValue("$academicYearId", history.AcademicYearId);
            command.Parameters.AddWithValue("$result", history.Result);
            command.Parameters.AddWithValue("$promotionDate", history.PromotionDate.ToString("yyyy-MM-dd HH:mm:ss"));

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

    public async Task<IReadOnlyList<StudentPromotionHistory>> GetByStudentAsync(
        int studentId,
        CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT PromotionID, StudentID, FromClassID, ToClassID, AcademicYearID, Result, PromotionDate
FROM tbl_StudentPromotionHistory
WHERE StudentID = $studentId
ORDER BY PromotionDate DESC;";

        await using var connection = new SqliteConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("$studentId", studentId);

        var result = new List<StudentPromotionHistory>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new StudentPromotionHistory
            {
                PromotionId = reader.GetInt32(0),
                StudentId = reader.GetInt32(1),
                FromClassId = reader.GetInt32(2),
                ToClassId = reader.IsDBNull(3) ? null : reader.GetInt32(3),
                AcademicYearId = reader.GetInt32(4),
                Result = reader.GetString(5),
                PromotionDate = DateTime.Parse(reader.GetString(6))
            });
        }

        return result;
    }

    public async Task<IReadOnlyList<PromotionHistoryDto>> GetHistoryAsync(
        int? academicYearId,
        int? studentId,
        CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT h.PromotionID,
       h.StudentID,
       s.FirstName || ' ' || s.LastName AS StudentName,
       s.RollNumber,
       fc.GradeName AS FromClassName,
       tc.GradeName AS ToClassName,
       ay.YearName AS AcademicYearName,
       h.Result,
       h.PromotionDate
FROM tbl_StudentPromotionHistory h
JOIN tbl_Students s ON h.StudentID = s.StudentID
JOIN tbl_Classes fc ON h.FromClassID = fc.ClassID
LEFT JOIN tbl_Classes tc ON h.ToClassID = tc.ClassID
JOIN tbl_AcademicYears ay ON h.AcademicYearID = ay.AcademicYearID
WHERE (@academicYearId IS NULL OR h.AcademicYearID = @academicYearId)
  AND (@studentId IS NULL OR h.StudentID = @studentId)
ORDER BY h.PromotionDate DESC;";

        await using var connection = new SqliteConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("@academicYearId", (object?)academicYearId ?? DBNull.Value);
        command.Parameters.AddWithValue("@studentId", (object?)studentId ?? DBNull.Value);

        var result = new List<PromotionHistoryDto>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new PromotionHistoryDto
            {
                PromotionId = reader.GetInt32(0),
                StudentId = reader.GetInt32(1),
                StudentName = reader.GetString(2),
                RollNumber = reader.GetString(3),
                FromClassName = reader.GetString(4),
                ToClassName = reader.IsDBNull(5) ? null : reader.GetString(5),
                AcademicYearName = reader.GetString(6),
                Result = reader.GetString(7),
                PromotionDate = DateTime.Parse(reader.GetString(8))
            });
        }

        return result;
    }
}
