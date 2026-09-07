using Microsoft.Data.Sqlite;
using Maktab.Application.Abstractions;
using Maktab.Domain.Entities;
using Maktab.Domain.Rules;

namespace Maktab.Infrastructure.Persistence;

public sealed class SqliteExamMarkRepository(IConnectionStringProvider connectionStringProvider) : IExamMarkRepository
{
    public async Task<IReadOnlyList<ExamMark>> GetMarksByClassAndSubjectAsync(int classId, int subjectId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT m.StudentID, m.SubjectID, m.MidtermScore, m.FinalScore, m.AcademicYearId
FROM tbl_ExamMarks m
INNER JOIN tbl_Students s ON m.StudentID = s.StudentID
WHERE s.ClassID = $classId AND m.SubjectID = $subjectId;";
        await using var connection = new SqliteConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("$classId", classId);
        command.Parameters.AddWithValue("$subjectId", subjectId);
        return await ReadMarksAsync(command, cancellationToken);
    }

    public async Task<IReadOnlyList<ExamMark>> GetMarksByClassSubjectAndYearAsync(int classId, int subjectId, int academicYearId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT m.StudentID, m.SubjectID, m.MidtermScore, m.FinalScore, m.AcademicYearId
FROM tbl_ExamMarks m
INNER JOIN tbl_Students s ON m.StudentID = s.StudentID
WHERE s.ClassID = $classId AND m.SubjectID = $subjectId AND m.AcademicYearId = $academicYearId;";
        await using var connection = new SqliteConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("$classId", classId);
        command.Parameters.AddWithValue("$subjectId", subjectId);
        command.Parameters.AddWithValue("$academicYearId", academicYearId);
        return await ReadMarksAsync(command, cancellationToken);
    }

    public async Task<IReadOnlyList<ExamMark>> GetMarksByStudentAsync(int studentId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT StudentID, SubjectID, MidtermScore, FinalScore, AcademicYearId
FROM tbl_ExamMarks
WHERE StudentID = $studentId;";
        await using var connection = new SqliteConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("$studentId", studentId);
        return await ReadMarksAsync(command, cancellationToken);
    }

    public async Task<IReadOnlyList<ExamMark>> GetMarksByStudentAndYearAsync(int studentId, int academicYearId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT StudentID, SubjectID, MidtermScore, FinalScore, AcademicYearId
FROM tbl_ExamMarks
WHERE StudentID = $studentId AND AcademicYearId = $academicYearId;";
        await using var connection = new SqliteConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("$studentId", studentId);
        command.Parameters.AddWithValue("$academicYearId", academicYearId);
        return await ReadMarksAsync(command, cancellationToken);
    }

    public async Task<IReadOnlyList<ExamMark>> GetMarksByClassAsync(int classId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT m.StudentID, m.SubjectID, m.MidtermScore, m.FinalScore, m.AcademicYearId
FROM tbl_ExamMarks m
INNER JOIN tbl_Students s ON m.StudentID = s.StudentID
WHERE s.ClassID = $classId;";
        await using var connection = new SqliteConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("$classId", classId);
        return await ReadMarksAsync(command, cancellationToken);
    }

    public async Task SaveOrUpdateMarkAsync(ExamMark mark, CancellationToken cancellationToken = default)
    {
        await SaveOrUpdateMarksBatchAsync([mark], cancellationToken);
    }

    public async Task SaveOrUpdateMarksBatchAsync(IEnumerable<ExamMark> marks, CancellationToken cancellationToken = default)
    {
        const string upsertSql = @"
INSERT INTO tbl_ExamMarks (StudentID, SubjectID, MidtermScore, FinalScore, TotalScore, AcademicYearId)
VALUES ($studentId, $subjectId, $midtermScore, $finalScore, $totalScore, $academicYearId)
ON CONFLICT(StudentID, SubjectID, AcademicYearId) DO UPDATE SET
    MidtermScore = excluded.MidtermScore,
    FinalScore = excluded.FinalScore,
    TotalScore = excluded.TotalScore;";

        await using var connection = new SqliteConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            foreach (var mark in marks)
            {
                var total = mark.MidtermScore + mark.FinalScore;
                await using var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = upsertSql;
                command.Parameters.AddWithValue("$studentId", mark.StudentId);
                command.Parameters.AddWithValue("$subjectId", mark.SubjectId);
                command.Parameters.AddWithValue("$midtermScore", (double)mark.MidtermScore);
                command.Parameters.AddWithValue("$finalScore", (double)mark.FinalScore);
                command.Parameters.AddWithValue("$totalScore", (double)total);
                command.Parameters.AddWithValue("$academicYearId", mark.AcademicYearId);
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

    private static async Task<IReadOnlyList<ExamMark>> ReadMarksAsync(SqliteCommand command, CancellationToken cancellationToken)
    {
        var result = new List<ExamMark>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new ExamMark
            {
                StudentId = reader.GetInt32(0),
                SubjectId = reader.GetInt32(1),
                MidtermScore = Convert.ToDecimal(reader.GetDouble(2)),
                FinalScore = Convert.ToDecimal(reader.GetDouble(3)),
                AcademicYearId = reader.GetInt32(4)
            });
        }
        return result;
    }
}
