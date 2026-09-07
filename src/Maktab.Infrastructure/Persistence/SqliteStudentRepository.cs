using Microsoft.Data.Sqlite;
using Maktab.Application.Abstractions;
using Maktab.Domain.Entities;

namespace Maktab.Infrastructure.Persistence;

public sealed class SqliteStudentRepository(IConnectionStringProvider connectionStringProvider) : IStudentRepository
{
    public async Task<IReadOnlyList<Student>> GetStudentsAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT StudentID, AdmissionNumber, FirstName, LastName, FatherName, ClassID, RollNumber, RegistrationDate
FROM tbl_Students
ORDER BY LastName, FirstName;";
        return await QueryStudentsAsync(sql, null, cancellationToken);
    }

    public async Task<IReadOnlyList<Student>> GetStudentsByClassAsync(int classId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT StudentID, AdmissionNumber, FirstName, LastName, FatherName, ClassID, RollNumber, RegistrationDate
FROM tbl_Students
WHERE ClassID = $classId
ORDER BY RollNumber;";
        return await QueryStudentsAsync(sql, command => command.Parameters.AddWithValue("$classId", classId), cancellationToken);
    }

    public async Task<IReadOnlyList<Student>> GetStudentsByClassAndAcademicYearAsync(int classId, int academicYearId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT s.StudentID, s.AdmissionNumber, s.FirstName, s.LastName, s.FatherName,
       e.ClassID, e.RollNumber, s.RegistrationDate
FROM tbl_Students s
INNER JOIN tbl_StudentAcademicEnrollments e ON e.StudentID = s.StudentID
WHERE e.ClassID = $classId
  AND e.AcademicYearID = $academicYearId
  AND e.Status IN ('Active', 'Promoted', 'Completed')
ORDER BY e.RollNumber;";
        return await QueryStudentsAsync(sql, command =>
        {
            command.Parameters.AddWithValue("$classId", classId);
            command.Parameters.AddWithValue("$academicYearId", academicYearId);
        }, cancellationToken);
    }

    public async Task<IReadOnlyList<StudentAcademicEnrollment>> GetStudentAcademicHistoryAsync(int studentId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT EnrollmentID, StudentID, AcademicYearID, ClassID, RollNumber, EnrollmentDate, Status
FROM tbl_StudentAcademicEnrollments
WHERE StudentID = $studentId
ORDER BY AcademicYearID;";
        await using var connection = new SqliteConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("$studentId", studentId);
        var result = new List<StudentAcademicEnrollment>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) result.Add(MapEnrollment(reader));
        return result;
    }

    public async Task<Student?> GetStudentByIdAsync(int studentId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT StudentID, AdmissionNumber, FirstName, LastName, FatherName, ClassID, RollNumber, RegistrationDate
FROM tbl_Students
WHERE StudentID = $studentId;";
        await using var connection = new SqliteConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("$studentId", studentId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapStudent(reader) : null;
    }

    public async Task<int> CreateStudentAsync(Student student, CancellationToken cancellationToken = default)
    {
        const string sql = @"
INSERT INTO tbl_Students (AdmissionNumber, FirstName, LastName, FatherName, ClassID, RollNumber, RegistrationDate)
VALUES ($admissionNumber, $firstName, $lastName, $fatherName, $classId, $rollNumber, $registrationDate);
SELECT last_insert_rowid();";
        await using var connection = new SqliteConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = sql;
            command.Parameters.AddWithValue("$admissionNumber", GenerateAdmissionNumber());
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
                if (Convert.ToInt32(await checkBooksCmd.ExecuteScalarAsync(cancellationToken)) > 0)
                    throw new InvalidOperationException("این شاگرد دارای سوابق امانت‌دهی کتاب در کتابخانه است و قابل حذف نیست. ابتدا باید سوابق امانت‌دهی وی را بررسی یا حذف کنید.");
            }
            await using (var checkTextbooksCmd = connection.CreateCommand())
            {
                checkTextbooksCmd.Transaction = transaction;
                checkTextbooksCmd.CommandText = "SELECT COUNT(1) FROM tbl_TextbookIssues WHERE StudentID = $studentId;";
                checkTextbooksCmd.Parameters.AddWithValue("$studentId", studentId);
                if (Convert.ToInt32(await checkTextbooksCmd.ExecuteScalarAsync(cancellationToken)) > 0)
                    throw new InvalidOperationException("این شاگرد دارای سوابق دریافت کتاب‌های درسی است و قابل حذف نیست. ابتدا باید سوابق کتاب‌های درسی وی را بررسی یا حذف کنید.");
            }
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "DELETE FROM tbl_Students WHERE StudentID = $studentId;";
            command.Parameters.AddWithValue("$studentId", studentId);
            if (await command.ExecuteNonQueryAsync(cancellationToken) == 0)
                throw new InvalidOperationException("شاگرد مورد نظر یافت نشد.");
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
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) > 0;
    }

    private async Task<IReadOnlyList<Student>> QueryStudentsAsync(string sql, Action<SqliteCommand>? configure, CancellationToken cancellationToken)
    {
        await using var connection = new SqliteConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        configure?.Invoke(command);
        var result = new List<Student>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) result.Add(MapStudent(reader));
        return result;
    }

    private static string GenerateAdmissionNumber() => $"ADM-{Guid.NewGuid():N}".ToUpperInvariant();

    private static async Task UpsertActiveEnrollmentAsync(SqliteConnection connection, SqliteTransaction transaction, int studentId, Student student, CancellationToken cancellationToken)
    {
        const string sql = @"
INSERT INTO tbl_StudentAcademicEnrollments
    (StudentID, AcademicYearID, ClassID, RollNumber, EnrollmentDate, Status)
SELECT $studentId, AcademicYearID, $classId, $rollNumber, $enrollmentDate, 'Active'
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

    private static Student MapStudent(SqliteDataReader reader) => new()
    {
        StudentId = reader.GetInt32(0),
        AdmissionNumber = reader.IsDBNull(1) ? null : reader.GetString(1),
        FirstName = reader.GetString(2),
        LastName = reader.GetString(3),
        FatherName = reader.GetString(4),
        ClassId = reader.GetInt32(5),
        RollNumber = reader.GetString(6),
        RegistrationDate = DateTime.Parse(reader.GetString(7))
    };

    private static StudentAcademicEnrollment MapEnrollment(SqliteDataReader reader) => new()
    {
        EnrollmentId = reader.GetInt32(0),
        StudentId = reader.GetInt32(1),
        AcademicYearId = reader.GetInt32(2),
        ClassId = reader.GetInt32(3),
        RollNumber = reader.GetString(4),
        EnrollmentDate = DateTime.Parse(reader.GetString(5)),
        Status = reader.GetString(6)
    };
}
