using Microsoft.Data.Sqlite;
using Maktab.Application.Abstractions;
using Maktab.Domain.Entities;

namespace Maktab.Infrastructure.Persistence;

public sealed class SqlitePromotionTransactionRepository(IConnectionStringProvider connectionStringProvider) : IPromotionTransactionRepository
{
    public async Task ApplyAsync(
        Student student,
        StudentPromotionHistory history,
        StudentAcademicEnrollment? targetEnrollment,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            await using (var studentCommand = connection.CreateCommand())
            {
                studentCommand.Transaction = transaction;
                studentCommand.CommandText = @"
UPDATE tbl_Students
SET ClassID = $classId,
    RollNumber = $rollNumber
WHERE StudentID = $studentId;";
                studentCommand.Parameters.AddWithValue("$classId", student.ClassId);
                studentCommand.Parameters.AddWithValue("$rollNumber", student.RollNumber);
                studentCommand.Parameters.AddWithValue("$studentId", student.StudentId);

                if (await studentCommand.ExecuteNonQueryAsync(cancellationToken) != 1)
                    throw new InvalidOperationException($"Student {student.StudentId} was not found.");
            }

            if (targetEnrollment != null)
            {
                await using var enrollmentCommand = connection.CreateCommand();
                enrollmentCommand.Transaction = transaction;
                enrollmentCommand.CommandText = @"
INSERT INTO tbl_StudentAcademicEnrollments
    (StudentID, AcademicYearID, ClassID, RollNumber, EnrollmentDate, Status)
VALUES
    ($studentId, $academicYearId, $classId, $rollNumber, $enrollmentDate, $status)
ON CONFLICT(StudentID, AcademicYearID) DO UPDATE SET
    ClassID = excluded.ClassID,
    RollNumber = excluded.RollNumber,
    EnrollmentDate = excluded.EnrollmentDate,
    Status = excluded.Status;";
                enrollmentCommand.Parameters.AddWithValue("$studentId", targetEnrollment.StudentId);
                enrollmentCommand.Parameters.AddWithValue("$academicYearId", targetEnrollment.AcademicYearId);
                enrollmentCommand.Parameters.AddWithValue("$classId", targetEnrollment.ClassId);
                enrollmentCommand.Parameters.AddWithValue("$rollNumber", targetEnrollment.RollNumber);
                enrollmentCommand.Parameters.AddWithValue("$enrollmentDate", targetEnrollment.EnrollmentDate.ToString("yyyy-MM-dd HH:mm:ss"));
                enrollmentCommand.Parameters.AddWithValue("$status", targetEnrollment.Status);
                await enrollmentCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            await using (var historyCommand = connection.CreateCommand())
            {
                historyCommand.Transaction = transaction;
                historyCommand.CommandText = @"
INSERT INTO tbl_StudentPromotionHistory
    (StudentID, FromClassID, ToClassID, AcademicYearID, Result, PromotionDate)
VALUES
    ($studentId, $fromClassId, $toClassId, $academicYearId, $result, $promotionDate);";
                historyCommand.Parameters.AddWithValue("$studentId", history.StudentId);
                historyCommand.Parameters.AddWithValue("$fromClassId", history.FromClassId);
                historyCommand.Parameters.AddWithValue("$toClassId", (object?)history.ToClassId ?? DBNull.Value);
                historyCommand.Parameters.AddWithValue("$academicYearId", history.AcademicYearId);
                historyCommand.Parameters.AddWithValue("$result", history.Result);
                historyCommand.Parameters.AddWithValue("$promotionDate", history.PromotionDate.ToString("yyyy-MM-dd HH:mm:ss"));
                await historyCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
