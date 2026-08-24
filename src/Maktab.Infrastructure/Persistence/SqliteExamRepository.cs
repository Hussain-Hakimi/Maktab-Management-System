using Microsoft.Data.Sqlite;
using Maktab.Application.Abstractions;
using Maktab.Domain.Entities;
using Maktab.Domain.Enums;

namespace Maktab.Infrastructure.Persistence;

public sealed class SqliteExamRepository(IConnectionStringProvider connectionStringProvider) : IExamRepository
{
    public async Task<int> CreateAsync(Exam exam, CancellationToken cancellationToken = default)
    {
        const string sql = @"
INSERT INTO tbl_Exams (SubjectID, ClassID, AcademicYearID, ExamType, ExamDate, CreatedByTeacherUserID)
VALUES ($subjectId, $classId, $academicYearId, $examType, $examDate, $createdByTeacherUserId);
SELECT last_insert_rowid();";

        await using var connection = new SqliteConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = sql;
            command.Parameters.AddWithValue("$subjectId", exam.SubjectId);
            command.Parameters.AddWithValue("$classId", exam.ClassId);
            command.Parameters.AddWithValue("$academicYearId", exam.AcademicYearId);
            command.Parameters.AddWithValue("$examType", exam.ExamType.ToString());
            command.Parameters.AddWithValue("$examDate", exam.ExamDate.ToString("yyyy-MM-dd"));
            command.Parameters.AddWithValue("$createdByTeacherUserId", exam.CreatedByTeacherUserId);

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

    public async Task<IReadOnlyList<ExamDto>> GetByTeacherAsync(
        int teacherUserId,
        CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT e.ExamID, e.SubjectID, s.SubjectName, e.ClassID, c.GradeName, e.AcademicYearID, ay.YearName,
       e.ExamType, e.ExamDate, e.CreatedByTeacherUserID, u.FullName
FROM tbl_Exams e
JOIN tbl_Subjects s ON e.SubjectID = s.SubjectID
JOIN tbl_Classes c ON e.ClassID = c.ClassID
JOIN tbl_AcademicYears ay ON e.AcademicYearID = ay.AcademicYearID
JOIN tbl_Users u ON e.CreatedByTeacherUserID = u.UserID
WHERE e.CreatedByTeacherUserID = $teacherUserId
ORDER BY e.ExamDate DESC;";

        await using var connection = new SqliteConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("$teacherUserId", teacherUserId);

        var result = new List<ExamDto>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(MapExam(reader));
        }

        return result;
    }

    public async Task<IReadOnlyList<ExamDto>> GetByClassSubjectAsync(
        int classId,
        int subjectId,
        int academicYearId,
        CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT e.ExamID, e.SubjectID, s.SubjectName, e.ClassID, c.GradeName, e.AcademicYearID, ay.YearName,
       e.ExamType, e.ExamDate, e.CreatedByTeacherUserID, u.FullName
FROM tbl_Exams e
JOIN tbl_Subjects s ON e.SubjectID = s.SubjectID
JOIN tbl_Classes c ON e.ClassID = c.ClassID
JOIN tbl_AcademicYears ay ON e.AcademicYearID = ay.AcademicYearID
JOIN tbl_Users u ON e.CreatedByTeacherUserID = u.UserID
WHERE e.ClassID = $classId AND e.SubjectID = $subjectId AND e.AcademicYearID = $academicYearId
ORDER BY e.ExamDate DESC;";

        await using var connection = new SqliteConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("$classId", classId);
        command.Parameters.AddWithValue("$subjectId", subjectId);
        command.Parameters.AddWithValue("$academicYearId", academicYearId);

        var result = new List<ExamDto>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(MapExam(reader));
        }

        return result;
    }

    public async Task DeleteAsync(int examId, CancellationToken cancellationToken = default)
    {
        const string sql = "DELETE FROM tbl_Exams WHERE ExamID = $examId;";

        await using var connection = new SqliteConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = sql;
            command.Parameters.AddWithValue("$examId", examId);

            var affected = await command.ExecuteNonQueryAsync(cancellationToken);
            if (affected == 0) throw new InvalidOperationException("Exam not found.");

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static ExamDto MapExam(SqliteDataReader reader)
    {
        return new ExamDto
        {
            ExamId = reader.GetInt32(0),
            SubjectId = reader.GetInt32(1),
            SubjectName = reader.GetString(2),
            ClassId = reader.GetInt32(3),
            ClassName = reader.GetString(4),
            AcademicYearId = reader.GetInt32(5),
            AcademicYearName = reader.GetString(6),
            ExamType = Enum.Parse<ExamType>(reader.GetString(7)),
            ExamDate = DateTime.Parse(reader.GetString(8)),
            CreatedByTeacherUserId = reader.GetInt32(9),
            CreatedByTeacherName = reader.GetString(10)
        };
    }
}
