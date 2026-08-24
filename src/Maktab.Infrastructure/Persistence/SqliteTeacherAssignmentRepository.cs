using Microsoft.Data.Sqlite;
using Maktab.Application.Abstractions;
using Maktab.Domain.Entities;

namespace Maktab.Infrastructure.Persistence;

public sealed class SqliteTeacherAssignmentRepository(
    IConnectionStringProvider connectionStringProvider) : ITeacherAssignmentRepository
{
    // ---------- Teacher Subjects ----------

    public async Task<int> AddTeacherSubjectAsync(
        TeacherSubject assignment,
        CancellationToken cancellationToken = default)
    {
        const string sql = @"
INSERT INTO tbl_TeacherSubjects (TeacherUserID, ClassID, SubjectID)
VALUES ($teacherUserId, $classId, $subjectId);
SELECT last_insert_rowid();";

        await using var connection = new SqliteConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = sql;
            command.Parameters.AddWithValue("$teacherUserId", assignment.TeacherUserId);
            command.Parameters.AddWithValue("$classId", assignment.ClassId);
            command.Parameters.AddWithValue("$subjectId", assignment.SubjectId);

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

    public async Task RemoveTeacherSubjectAsync(
        int teacherSubjectId,
        CancellationToken cancellationToken = default)
    {
        const string sql = "DELETE FROM tbl_TeacherSubjects WHERE TeacherSubjectID = $id;";

        await using var connection = new SqliteConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = sql;
            command.Parameters.AddWithValue("$id", teacherSubjectId);

            var affected = await command.ExecuteNonQueryAsync(cancellationToken);
            if (affected == 0) throw new InvalidOperationException("Teacher subject assignment not found.");

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<IReadOnlyList<TeacherSubjectAssignmentDto>> GetTeacherSubjectsAsync(
        int? teacherUserId = null,
        CancellationToken cancellationToken = default)
    {
        var sql = @"
SELECT ts.TeacherSubjectID, ts.TeacherUserID, u.FullName, ts.ClassID, c.GradeName, ts.SubjectID, s.SubjectName
FROM tbl_TeacherSubjects ts
JOIN tbl_Users u ON ts.TeacherUserID = u.UserID
JOIN tbl_Classes c ON ts.ClassID = c.ClassID
JOIN tbl_Subjects s ON ts.SubjectID = s.SubjectID
WHERE (@teacherUserId IS NULL OR ts.TeacherUserID = @teacherUserId)
ORDER BY u.FullName, c.GradeName, s.SubjectName;";

        await using var connection = new SqliteConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("@teacherUserId", (object?)teacherUserId ?? DBNull.Value);

        var result = new List<TeacherSubjectAssignmentDto>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new TeacherSubjectAssignmentDto
            {
                TeacherSubjectId = reader.GetInt32(0),
                TeacherUserId = reader.GetInt32(1),
                TeacherName = reader.GetString(2),
                ClassId = reader.GetInt32(3),
                ClassName = reader.GetString(4),
                SubjectId = reader.GetInt32(5),
                SubjectName = reader.GetString(6)
            });
        }

        return result;
    }

    public async Task<IReadOnlyList<TeacherSubjectAssignmentDto>> GetTeacherSubjectsByTeacherAsync(
        int teacherUserId,
        CancellationToken cancellationToken = default)
    {
        return await GetTeacherSubjectsAsync(teacherUserId, cancellationToken);
    }

    // ---------- Class Guardians ----------

    public async Task<int> AddClassGuardianAsync(
        ClassGuardian guardian,
        CancellationToken cancellationToken = default)
    {
        const string sql = @"
INSERT INTO tbl_ClassGuardians (TeacherUserID, ClassID)
VALUES ($teacherUserId, $classId);
SELECT last_insert_rowid();";

        await using var connection = new SqliteConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = sql;
            command.Parameters.AddWithValue("$teacherUserId", guardian.TeacherUserId);
            command.Parameters.AddWithValue("$classId", guardian.ClassId);

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

    public async Task RemoveClassGuardianAsync(
        int classGuardianId,
        CancellationToken cancellationToken = default)
    {
        const string sql = "DELETE FROM tbl_ClassGuardians WHERE ClassGuardianID = $id;";

        await using var connection = new SqliteConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = sql;
            command.Parameters.AddWithValue("$id", classGuardianId);

            var affected = await command.ExecuteNonQueryAsync(cancellationToken);
            if (affected == 0) throw new InvalidOperationException("Class guardian assignment not found.");

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<IReadOnlyList<ClassGuardianDto>> GetClassGuardiansAsync(
        int? teacherUserId = null,
        CancellationToken cancellationToken = default)
    {
        var sql = @"
SELECT cg.ClassGuardianID, cg.TeacherUserID, u.FullName, cg.ClassID, c.GradeName
FROM tbl_ClassGuardians cg
JOIN tbl_Users u ON cg.TeacherUserID = u.UserID
JOIN tbl_Classes c ON cg.ClassID = c.ClassID
WHERE (@teacherUserId IS NULL OR cg.TeacherUserID = @teacherUserId)
ORDER BY u.FullName, c.GradeName;";

        await using var connection = new SqliteConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("@teacherUserId", (object?)teacherUserId ?? DBNull.Value);

        var result = new List<ClassGuardianDto>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new ClassGuardianDto
            {
                ClassGuardianId = reader.GetInt32(0),
                TeacherUserId = reader.GetInt32(1),
                TeacherName = reader.GetString(2),
                ClassId = reader.GetInt32(3),
                ClassName = reader.GetString(4)
            });
        }

        return result;
    }

    public async Task<ClassGuardianDto?> GetClassGuardianByTeacherAndClassAsync(
        int teacherUserId,
        int classId,
        CancellationToken cancellationToken = default)
    {
        var all = await GetClassGuardiansAsync(teacherUserId, cancellationToken);
        return all.FirstOrDefault(g => g.ClassId == classId);
    }
}
