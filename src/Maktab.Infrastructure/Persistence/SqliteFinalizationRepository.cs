using Microsoft.Data.Sqlite;
using Maktab.Application.Abstractions;
using Maktab.Domain.Entities;

namespace Maktab.Infrastructure.Persistence;

public sealed class SqliteFinalizationRepository(IConnectionStringProvider connectionStringProvider) : IFinalizationRepository
{
    public async Task<ClassFinalization?> GetByClassYearAsync(
        int classId,
        int academicYearId,
        CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT ClassFinalizationID, ClassID, AcademicYearID, IsFinalized, FinalizedByTeacherUserID, FinalizationDate
FROM tbl_ClassFinalizations
WHERE ClassID = $classId AND AcademicYearID = $academicYearId;";

        await using var connection = new SqliteConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("$classId", classId);
        command.Parameters.AddWithValue("$academicYearId", academicYearId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return new ClassFinalization
            {
                ClassFinalizationId = reader.GetInt32(0),
                ClassId = reader.GetInt32(1),
                AcademicYearId = reader.GetInt32(2),
                IsFinalized = reader.GetInt32(3) == 1,
                FinalizedByTeacherUserId = reader.GetInt32(4),
                FinalizationDate = DateTime.Parse(reader.GetString(5))
            };
        }

        return null;
    }

    public async Task UpsertAsync(
        ClassFinalization finalization,
        CancellationToken cancellationToken = default)
    {
        const string sql = @"
INSERT INTO tbl_ClassFinalizations (ClassID, AcademicYearID, IsFinalized, FinalizedByTeacherUserID, FinalizationDate)
VALUES ($classId, $academicYearId, $isFinalized, $finalizedBy, $date)
ON CONFLICT(ClassID, AcademicYearID) DO UPDATE SET
    IsFinalized = excluded.IsFinalized,
    FinalizedByTeacherUserID = excluded.FinalizedByTeacherUserID,
    FinalizationDate = excluded.FinalizationDate;";

        await using var connection = new SqliteConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = sql;
            command.Parameters.AddWithValue("$classId", finalization.ClassId);
            command.Parameters.AddWithValue("$academicYearId", finalization.AcademicYearId);
            command.Parameters.AddWithValue("$isFinalized", finalization.IsFinalized ? 1 : 0);
            command.Parameters.AddWithValue("$finalizedBy", finalization.FinalizedByTeacherUserId);
            command.Parameters.AddWithValue("$date", finalization.FinalizationDate.ToString("yyyy-MM-dd HH:mm:ss"));

            await command.ExecuteNonQueryAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
