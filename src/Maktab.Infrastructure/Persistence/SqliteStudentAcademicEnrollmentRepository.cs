using Microsoft.Data.Sqlite;
using Maktab.Application.Abstractions;
using Maktab.Domain.Entities;

namespace Maktab.Infrastructure.Persistence;

public sealed class SqliteStudentAcademicEnrollmentRepository(IConnectionStringProvider connectionStringProvider)
    : IStudentAcademicEnrollmentRepository
{
    public async Task<StudentAcademicEnrollment?> GetByStudentAndAcademicYearAsync(
        int studentId,
        int academicYearId,
        CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT EnrollmentID, StudentID, AcademicYearID, ClassID, RollNumber, EnrollmentDate, Status
FROM tbl_StudentAcademicEnrollments
WHERE StudentID = $studentId AND AcademicYearID = $academicYearId;";

        await using var connection = new SqliteConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("$studentId", studentId);
        command.Parameters.AddWithValue("$academicYearId", academicYearId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? Map(reader) : null;
    }

    public async Task<IReadOnlyList<StudentAcademicEnrollment>> GetByAcademicYearAsync(
        int academicYearId,
        CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT EnrollmentID, StudentID, AcademicYearID, ClassID, RollNumber, EnrollmentDate, Status
FROM tbl_StudentAcademicEnrollments
WHERE AcademicYearID = $academicYearId
ORDER BY ClassID, RollNumber;";

        return await QueryAsync(sql, command =>
            command.Parameters.AddWithValue("$academicYearId", academicYearId), cancellationToken);
    }

    public async Task<IReadOnlyList<StudentAcademicEnrollment>> GetByClassAndAcademicYearAsync(
        int classId,
        int academicYearId,
        CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT EnrollmentID, StudentID, AcademicYearID, ClassID, RollNumber, EnrollmentDate, Status
FROM tbl_StudentAcademicEnrollments
WHERE ClassID = $classId AND AcademicYearID = $academicYearId
ORDER BY RollNumber;";

        return await QueryAsync(sql, command =>
        {
            command.Parameters.AddWithValue("$classId", classId);
            command.Parameters.AddWithValue("$academicYearId", academicYearId);
        }, cancellationToken);
    }

    public async Task<int> CreateOrUpdateAsync(
        StudentAcademicEnrollment enrollment,
        CancellationToken cancellationToken = default)
    {
        const string sql = @"
INSERT INTO tbl_StudentAcademicEnrollments
    (StudentID, AcademicYearID, ClassID, RollNumber, EnrollmentDate, Status)
VALUES
    ($studentId, $academicYearId, $classId, $rollNumber, $enrollmentDate, $status)
ON CONFLICT(StudentID, AcademicYearID) DO UPDATE SET
    ClassID = excluded.ClassID,
    RollNumber = excluded.RollNumber,
    Status = excluded.Status; 
SELECT EnrollmentID
FROM tbl_StudentAcademicEnrollments
WHERE StudentID = $studentId AND AcademicYearID = $academicYearId;";

        await using var connection = new SqliteConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("$studentId", enrollment.StudentId);
        command.Parameters.AddWithValue("$academicYearId", enrollment.AcademicYearId);
        command.Parameters.AddWithValue("$classId", enrollment.ClassId);
        command.Parameters.AddWithValue("$rollNumber", enrollment.RollNumber);
        command.Parameters.AddWithValue("$enrollmentDate", enrollment.EnrollmentDate.ToString("yyyy-MM-dd HH:mm:ss"));
        command.Parameters.AddWithValue("$status", enrollment.Status);

        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
    }

    private async Task<IReadOnlyList<StudentAcademicEnrollment>> QueryAsync(
        string sql,
        Action<SqliteCommand> configure,
        CancellationToken cancellationToken)
    {
        await using var connection = new SqliteConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        configure(command);

        var result = new List<StudentAcademicEnrollment>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(Map(reader));
        }

        return result;
    }

    private static StudentAcademicEnrollment Map(SqliteDataReader reader)
    {
        return new StudentAcademicEnrollment
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
}
