using Microsoft.Data.Sqlite;
using Maktab.Application.Abstractions;
using Maktab.Domain.Entities;

namespace Maktab.Infrastructure.Persistence;

public sealed class SqliteStudentRepository(IConnectionStringProvider connectionStringProvider) : IStudentRepository
{
    public async Task<IReadOnlyList<Student>> GetStudentsAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT StudentID, FirstName, LastName, FatherName, ClassID, RollNumber, RegistrationDate
FROM tbl_Students
ORDER BY LastName, FirstName;";

        await using var connection = new SqliteConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        var result = new List<Student>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(MapStudent(reader));
        }

        return result;
    }

    public async Task<IReadOnlyList<Student>> GetStudentsByClassAsync(int classId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT StudentID, FirstName, LastName, FatherName, ClassID, RollNumber, RegistrationDate
FROM tbl_Students
WHERE ClassID = $classId
ORDER BY RollNumber;";

        await using var connection = new SqliteConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("$classId", classId);

        var result = new List<Student>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(MapStudent(reader));
        }

        return result;
    }

    public async Task<Student?> GetStudentByIdAsync(int studentId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT StudentID, FirstName, LastName, FatherName, ClassID, RollNumber, RegistrationDate
FROM tbl_Students
WHERE StudentID = $studentId;";

        await using var connection = new SqliteConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("$studentId", studentId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return MapStudent(reader);
        }

        return null;
    }

    public async Task<int> CreateStudentAsync(Student student, CancellationToken cancellationToken = default)
    {
        const string sql = @"
INSERT INTO tbl_Students (FirstName, LastName, FatherName, ClassID, RollNumber, RegistrationDate)
VALUES ($firstName, $lastName, $fatherName, $classId, $rollNumber, $registrationDate);
SELECT last_insert_rowid();";

        await using var connection = new SqliteConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = sql;
            command.Parameters.AddWithValue("$firstName", student.FirstName);
            command.Parameters.AddWithValue("$lastName", student.LastName);
            command.Parameters.AddWithValue("$fatherName", student.FatherName);
            command.Parameters.AddWithValue("$classId", student.ClassId);
            command.Parameters.AddWithValue("$rollNumber", student.RollNumber);
            command.Parameters.AddWithValue("$registrationDate", student.RegistrationDate.ToString("yyyy-MM-dd HH:mm:ss"));

            var id = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
            await UpsertActiveEnrollmentAsync(connection, transaction, id, student, cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            return id;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task UpdateStudentAsync(Student student, CancellationToken cancellationToken = default)
    {
        const string sql = @"
UPDATE tbl_Students
SET FirstName = $firstName,
    LastName = $lastName,
    FatherName = $fatherName,
    ClassID = $classId,
    RollNumber = $rollNumber
WHERE StudentID = $studentId;";

        await using var connection = new SqliteConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = sql;
            command.Parameters.AddWithValue("$firstName", student.FirstName);
            command.Parameters.AddWithValue("$lastName", student.LastName);
            command.Parameters.AddWithValue("$fatherName", student.FatherName);
            command.Parameters.AddWithValue("$classId", student.ClassId);
            command.Parameters.AddWithValue("$rollNumber", student.RollNumber);
            command.Parameters.AddWithValue("$studentId", student.StudentId);

            var affected = await command.ExecuteNonQueryAsync(cancellationToken);
            if (affected == 0) throw new InvalidOperationException("Student not found.");

            await UpsertActiveEnrollmentAsync(connection, transaction, student.StudentId, student, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task DeleteStudentAsync(int studentId, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            await using (var checkBooksCmd = connection.CreateCommand())
            {
                checkBooksCmd.Transaction = transaction;
                checkBooksCmd.CommandText = "SELECT COUNT(1) FROM tbl_BookIssues WHERE StudentID = $studentId;";
                checkBooksCmd.Parameters.AddWithValue("$studentId", studentId);
                var bookIssuesCount = Convert.ToInt32(await checkBooksCmd.ExecuteScalarAsync(cancellationToken));
                if (bookIssuesCount > 0)
                {
                    throw new InvalidOperationException("این شاگرد دارای سوابق امانت‌دهی کتاب در کتابخانه است و قابل حذف نیست. ابتدا باید سوابق امانت‌دهی وی را بررسی یا حذف کنید.");
                }
            }

            await using (var checkTextbooksCmd = connection.CreateCommand())
            {
                checkTextbooksCmd.Transaction = transaction;
                checkTextbooksCmd.CommandText = "SELECT COUNT(1) FROM tbl_TextbookIssues WHERE StudentID = $studentId;";
                checkTextbooksCmd.Parameters.AddWithValue("$studentId", studentId);
                var textbookIssuesCount = Convert.ToInt32(await checkTextbooksCmd.ExecuteScalarAsync(cancellationToken));
                if (textbookIssuesCount > 0)
                {
                    throw new InvalidOperationException("این شاگرد دارای سوابق دریافت کتاب‌های درسی است و قابل حذف نیست. ابتدا باید سوابق کتاب‌های درسی وی را بررسی یا حذف کنید.");
                }
            }

            const string sql = "DELETE FROM tbl_Students WHERE StudentID = $studentId;";
            await using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = sql;
                command.Parameters.AddWithValue("$studentId", studentId);

                var affected = await command.ExecuteNonQueryAsync(cancellationToken);
                if (affected == 0) throw new InvalidOperationException("شاگرد مورد نظر یافت نشد.");
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<bool> ExistsByRollNumberAsync(int classId, string rollNumber, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT COUNT(1) FROM tbl_Students WHERE ClassID = $classId AND RollNumber = $rollNumber;";

        await using var connection = new SqliteConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("$classId", classId);
        command.Parameters.AddWithValue("$rollNumber", rollNumber);

        var count = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
        return count > 0;
    }

    private static async Task UpsertActiveEnrollmentAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        int studentId,
        Student student,
        CancellationToken cancellationToken)
    {
        const string sql = @"
INSERT INTO tbl_StudentAcademicEnrollments
    (StudentID, AcademicYearID, ClassID, RollNumber, EnrollmentDate, Status)
SELECT
    $studentId, AcademicYearID, $classId, $rollNumber, $enrollmentDate, 'Active'
FROM tbl_AcademicYears
WHERE IsActive = 1
ON CONFLICT(StudentID, AcademicYearID) DO UPDATE SET
    ClassID = excluded.ClassID,
    RollNumber = excluded.RollNumber,
    Status = 'Active';";

        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        command.Parameters.AddWithValue("$studentId", studentId);
        command.Parameters.AddWithValue("$classId", student.ClassId);
        command.Parameters.AddWithValue("$rollNumber", student.RollNumber);
        command.Parameters.AddWithValue("$enrollmentDate", student.RegistrationDate.ToString("yyyy-MM-dd HH:mm:ss"));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static Student MapStudent(SqliteDataReader reader)
    {
        return new Student
        {
            StudentId = reader.GetInt32(0),
            FirstName = reader.GetString(1),
            LastName = reader.GetString(2),
            FatherName = reader.GetString(3),
            ClassId = reader.GetInt32(4),
            RollNumber = reader.GetString(5),
            RegistrationDate = DateTime.Parse(reader.GetString(6))
        };
    }
}
