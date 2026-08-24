using Microsoft.Data.Sqlite;
using Maktab.Application.Abstractions;
using Maktab.Domain.Entities;

namespace Maktab.Infrastructure.Persistence;

public sealed class SqliteClassSubjectRepository(IConnectionStringProvider connectionStringProvider) : IClassSubjectRepository
{
    public async Task<IReadOnlyList<SchoolClass>> GetClassesAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT ClassID, GradeName, NumberOfSubjects
FROM tbl_Classes
ORDER BY ClassID;";

        await using var connection = new SqliteConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        var result = new List<SchoolClass>();

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new SchoolClass
            {
                ClassId = reader.GetInt32(0),
                GradeName = reader.GetString(1),
                NumberOfSubjects = reader.GetInt32(2)
            });
        }

        return result;
    }

    public async Task<IReadOnlyList<Subject>> GetSubjectsByClassAsync(int classId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT SubjectID, SubjectName, ClassID
FROM tbl_Subjects
WHERE ClassID = $classId
ORDER BY SubjectName;";

        await using var connection = new SqliteConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("$classId", classId);

        var result = new List<Subject>();

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new Subject
            {
                SubjectId = reader.GetInt32(0),
                SubjectName = reader.GetString(1),
                ClassId = reader.GetInt32(2)
            });
        }

        return result;
    }

    public async Task<IReadOnlyList<Subject>> GetAllSubjectsAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT SubjectID, SubjectName, ClassID
FROM tbl_Subjects
ORDER BY SubjectName;";

        await using var connection = new SqliteConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        var result = new List<Subject>();

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new Subject
            {
                SubjectId = reader.GetInt32(0),
                SubjectName = reader.GetString(1),
                ClassId = reader.GetInt32(2)
            });
        }

        return result;
    }

    public async Task<int> CreateClassAsync(SchoolClass schoolClass, CancellationToken cancellationToken = default)
    {
        const string sql = @"
INSERT INTO tbl_Classes (GradeName, NumberOfSubjects)
VALUES ($gradeName, $numberOfSubjects);
SELECT last_insert_rowid();";

        await using var connection = new SqliteConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = sql;
            command.Parameters.AddWithValue("$gradeName", schoolClass.GradeName);
            command.Parameters.AddWithValue("$numberOfSubjects", schoolClass.NumberOfSubjects);

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

    public async Task UpdateClassAsync(SchoolClass schoolClass, CancellationToken cancellationToken = default)
    {
        const string sql = @"
UPDATE tbl_Classes
SET GradeName = $gradeName,
    NumberOfSubjects = $numberOfSubjects
WHERE ClassID = $classId;";

        await using var connection = new SqliteConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = sql;
            command.Parameters.AddWithValue("$gradeName", schoolClass.GradeName);
            command.Parameters.AddWithValue("$numberOfSubjects", schoolClass.NumberOfSubjects);
            command.Parameters.AddWithValue("$classId", schoolClass.ClassId);

            var affected = await command.ExecuteNonQueryAsync(cancellationToken);
            if (affected == 0)
            {
                throw new InvalidOperationException("Class record was not found.");
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task DeleteClassAsync(int classId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
DELETE FROM tbl_Classes
WHERE ClassID = $classId;";

        await using var connection = new SqliteConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = sql;
            command.Parameters.AddWithValue("$classId", classId);

            var affected = await command.ExecuteNonQueryAsync(cancellationToken);
            if (affected == 0)
            {
                throw new InvalidOperationException("Class record was not found.");
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<int> CreateSubjectAsync(Subject subject, CancellationToken cancellationToken = default)
    {
        const string sql = @"
INSERT INTO tbl_Subjects (SubjectName, ClassID)
VALUES ($subjectName, $classId);
SELECT last_insert_rowid();";

        await using var connection = new SqliteConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = sql;
            command.Parameters.AddWithValue("$subjectName", subject.SubjectName);
            command.Parameters.AddWithValue("$classId", subject.ClassId);

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

    public async Task UpdateSubjectAsync(Subject subject, CancellationToken cancellationToken = default)
    {
        const string sql = @"
UPDATE tbl_Subjects
SET SubjectName = $subjectName,
    ClassID = $classId
WHERE SubjectID = $subjectId;";

        await using var connection = new SqliteConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = sql;
            command.Parameters.AddWithValue("$subjectName", subject.SubjectName);
            command.Parameters.AddWithValue("$classId", subject.ClassId);
            command.Parameters.AddWithValue("$subjectId", subject.SubjectId);

            var affected = await command.ExecuteNonQueryAsync(cancellationToken);
            if (affected == 0)
            {
                throw new InvalidOperationException("Subject record was not found.");
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task DeleteSubjectAsync(int subjectId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
DELETE FROM tbl_Subjects
WHERE SubjectID = $subjectId;";

        await using var connection = new SqliteConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = sql;
            command.Parameters.AddWithValue("$subjectId", subjectId);

            var affected = await command.ExecuteNonQueryAsync(cancellationToken);
            if (affected == 0)
            {
                throw new InvalidOperationException("Subject record was not found.");
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
