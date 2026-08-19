using Microsoft.Data.Sqlite;
using Maktab.Application.Abstractions;
using Maktab.Domain.Entities;
using Maktab.Domain.Enums;

namespace Maktab.Infrastructure.Persistence;

public sealed class SqliteTextbookRepository(IConnectionStringProvider connectionStringProvider) : ITextbookRepository
{
    public async Task<IReadOnlyList<Textbook>> GetTextbooksAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT TextbookID, Title, Subject, ClassID, TotalCopies, AvailableCopies
FROM tbl_Textbooks
ORDER BY Title;";

        await using var connection = new SqliteConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        var result = new List<Textbook>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(MapTextbook(reader));
        }

        return result;
    }

    public async Task<Textbook?> GetTextbookByIdAsync(int textbookId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT TextbookID, Title, Subject, ClassID, TotalCopies, AvailableCopies
FROM tbl_Textbooks
WHERE TextbookID = $textbookId;";

        await using var connection = new SqliteConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("$textbookId", textbookId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return MapTextbook(reader);
        }

        return null;
    }

    public async Task<int> CreateTextbookAsync(Textbook textbook, CancellationToken cancellationToken = default)
    {
        const string sql = @"
INSERT INTO tbl_Textbooks (Title, Subject, ClassID, TotalCopies, AvailableCopies)
VALUES ($title, $subject, $classId, $totalCopies, $availableCopies);
SELECT last_insert_rowid();";

        await using var connection = new SqliteConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = sql;
            command.Parameters.AddWithValue("$title", textbook.Title);
            command.Parameters.AddWithValue("$subject", (object?)textbook.Subject ?? DBNull.Value);
            command.Parameters.AddWithValue("$classId", (object?)textbook.ClassId ?? DBNull.Value);
            command.Parameters.AddWithValue("$totalCopies", textbook.TotalCopies);
            command.Parameters.AddWithValue("$availableCopies", textbook.AvailableCopies);

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

    public async Task UpdateTextbookAsync(Textbook textbook, CancellationToken cancellationToken = default)
    {
        const string sql = @"
UPDATE tbl_Textbooks
SET Title = $title,
    Subject = $subject,
    ClassID = $classId,
    TotalCopies = $totalCopies,
    AvailableCopies = $availableCopies
WHERE TextbookID = $textbookId;";

        await using var connection = new SqliteConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = sql;
            command.Parameters.AddWithValue("$title", textbook.Title);
            command.Parameters.AddWithValue("$subject", (object?)textbook.Subject ?? DBNull.Value);
            command.Parameters.AddWithValue("$classId", (object?)textbook.ClassId ?? DBNull.Value);
            command.Parameters.AddWithValue("$totalCopies", textbook.TotalCopies);
            command.Parameters.AddWithValue("$availableCopies", textbook.AvailableCopies);
            command.Parameters.AddWithValue("$textbookId", textbook.TextbookId);

            var affected = await command.ExecuteNonQueryAsync(cancellationToken);
            if (affected == 0) throw new InvalidOperationException("Textbook not found.");

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task DeleteTextbookAsync(int textbookId, CancellationToken cancellationToken = default)
    {
        const string sql = "DELETE FROM tbl_Textbooks WHERE TextbookID = $textbookId;";

        await using var connection = new SqliteConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = sql;
            command.Parameters.AddWithValue("$textbookId", textbookId);

            var affected = await command.ExecuteNonQueryAsync(cancellationToken);
            if (affected == 0) throw new InvalidOperationException("Textbook not found.");

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<IReadOnlyList<TextbookIssueDto>> GetIssuesAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT i.IssueID, i.TextbookID, t.Title, i.StudentID, s.FirstName || ' ' || s.LastName AS StudentName, s.RollNumber,
       i.IssueDate, i.ReturnDate, i.Status
FROM tbl_TextbookIssues i
JOIN tbl_Textbooks t ON i.TextbookID = t.TextbookID
JOIN tbl_Students s ON i.StudentID = s.StudentID
ORDER BY i.IssueDate DESC;";

        await using var connection = new SqliteConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        var result = new List<TextbookIssueDto>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(MapIssue(reader));
        }

        return result;
    }

    public async Task<IReadOnlyList<TextbookIssueDto>> GetActiveIssuesAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT i.IssueID, i.TextbookID, t.Title, i.StudentID, s.FirstName || ' ' || s.LastName AS StudentName, s.RollNumber,
       i.IssueDate, i.ReturnDate, i.Status
FROM tbl_TextbookIssues i
JOIN tbl_Textbooks t ON i.TextbookID = t.TextbookID
JOIN tbl_Students s ON i.StudentID = s.StudentID
WHERE i.Status = 'Issued'
ORDER BY i.IssueDate;";

        await using var connection = new SqliteConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        var result = new List<TextbookIssueDto>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(MapIssue(reader));
        }

        return result;
    }

    public async Task<int> IssueTextbookAsync(TextbookIssue issue, CancellationToken cancellationToken = default)
    {
        const string insertIssueSql = @"
INSERT INTO tbl_TextbookIssues (TextbookID, StudentID, IssueDate, Status)
VALUES ($textbookId, $studentId, $issueDate, 'Issued');
SELECT last_insert_rowid();";

        const string decrementAvailableSql = @"
UPDATE tbl_Textbooks
SET AvailableCopies = AvailableCopies - 1
WHERE TextbookID = $textbookId AND AvailableCopies > 0;
SELECT changes();";

        await using var connection = new SqliteConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            int issueId;
            await using (var insertCmd = connection.CreateCommand())
            {
                insertCmd.Transaction = transaction;
                insertCmd.CommandText = insertIssueSql;
                insertCmd.Parameters.AddWithValue("$textbookId", issue.TextbookId);
                insertCmd.Parameters.AddWithValue("$studentId", issue.StudentId);
                insertCmd.Parameters.AddWithValue("$issueDate", issue.IssueDate.ToString("yyyy-MM-dd"));
                issueId = Convert.ToInt32(await insertCmd.ExecuteScalarAsync(cancellationToken));
            }

            await using (var updateCmd = connection.CreateCommand())
            {
                updateCmd.Transaction = transaction;
                updateCmd.CommandText = decrementAvailableSql;
                updateCmd.Parameters.AddWithValue("$textbookId", issue.TextbookId);
                var changes = Convert.ToInt32(await updateCmd.ExecuteScalarAsync(cancellationToken));
                if (changes == 0) throw new InvalidOperationException("No available copies.");
            }

            await transaction.CommitAsync(cancellationToken);
            return issueId;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task ReturnTextbookAsync(int issueId, DateTime returnDate, CancellationToken cancellationToken = default)
    {
        const string updateIssueSql = @"
UPDATE tbl_TextbookIssues
SET ReturnDate = $returnDate,
    Status = 'Returned'
WHERE IssueID = $issueId AND Status = 'Issued';
SELECT changes();";

        const string incrementAvailableSql = @"
UPDATE tbl_Textbooks
SET AvailableCopies = AvailableCopies + 1
WHERE TextbookID = (SELECT TextbookID FROM tbl_TextbookIssues WHERE IssueID = $issueId);";

        await using var connection = new SqliteConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            await using (var updateCmd = connection.CreateCommand())
            {
                updateCmd.Transaction = transaction;
                updateCmd.CommandText = updateIssueSql;
                updateCmd.Parameters.AddWithValue("$returnDate", returnDate.ToString("yyyy-MM-dd"));
                updateCmd.Parameters.AddWithValue("$issueId", issueId);
                var changes = Convert.ToInt32(await updateCmd.ExecuteScalarAsync(cancellationToken));
                if (changes == 0) throw new InvalidOperationException("Issue not found or already returned.");
            }

            await using (var incCmd = connection.CreateCommand())
            {
                incCmd.Transaction = transaction;
                incCmd.CommandText = incrementAvailableSql;
                incCmd.Parameters.AddWithValue("$issueId", issueId);
                await incCmd.ExecuteNonQueryAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static Textbook MapTextbook(SqliteDataReader reader)
    {
        return new Textbook
        {
            TextbookId = reader.GetInt32(0),
            Title = reader.GetString(1),
            Subject = reader.IsDBNull(2) ? null : reader.GetString(2),
            ClassId = reader.IsDBNull(3) ? null : reader.GetInt32(3),
            TotalCopies = reader.GetInt32(4),
            AvailableCopies = reader.GetInt32(5)
        };
    }

    private static TextbookIssueDto MapIssue(SqliteDataReader reader)
    {
        return new TextbookIssueDto
        {
            IssueId = reader.GetInt32(0),
            TextbookId = reader.GetInt32(1),
            TextbookTitle = reader.GetString(2),
            StudentId = reader.GetInt32(3),
            StudentName = reader.GetString(4),
            RollNumber = reader.GetString(5),
            IssueDate = DateTime.Parse(reader.GetString(6)),
            ReturnDate = reader.IsDBNull(7) ? null : DateTime.Parse(reader.GetString(7)),
            Status = reader.GetString(8) == "Returned" ? TextbookIssueStatus.Returned : TextbookIssueStatus.Issued
        };
    }
}
