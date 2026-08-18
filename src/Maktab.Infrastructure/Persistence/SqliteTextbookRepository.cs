using Microsoft.Data.Sqlite;
using Maktab.Application.Abstractions;
using Maktab.Domain.Entities;

namespace Maktab.Infrastructure.Persistence;

public sealed class SqliteTextbookRepository(IConnectionStringProvider connectionStringProvider) : ITextbookRepository
{
    public async Task<IReadOnlyList<Textbook>> GetTextbooksAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT TextbookID, Title, SubjectName, GradeLevel, TotalCopies, AvailableCopies
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
SELECT TextbookID, Title, SubjectName, GradeLevel, TotalCopies, AvailableCopies
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
INSERT INTO tbl_Textbooks (Title, SubjectName, GradeLevel, TotalCopies, AvailableCopies)
VALUES ($title, $subjectName, $gradeLevel, $totalCopies, $availableCopies);
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
            command.Parameters.AddWithValue("$subjectName", (object?)textbook.SubjectName ?? DBNull.Value);
            command.Parameters.AddWithValue("$gradeLevel", (object?)textbook.GradeLevel ?? DBNull.Value);
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
    SubjectName = $subjectName,
    GradeLevel = $gradeLevel,
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
            command.Parameters.AddWithValue("$subjectName", (object?)textbook.SubjectName ?? DBNull.Value);
            command.Parameters.AddWithValue("$gradeLevel", (object?)textbook.GradeLevel ?? DBNull.Value);
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

    public async Task<IReadOnlyList<TextbookIssue>> GetIssuesAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT IssueID, TextbookID, StudentID, IssueDate, ReturnDate
FROM tbl_TextbookIssues
ORDER BY IssueID DESC;";

        return await QueryIssuesAsync(sql, null, cancellationToken);
    }

    public async Task<IReadOnlyList<TextbookIssue>> GetActiveIssuesAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT IssueID, TextbookID, StudentID, IssueDate, ReturnDate
FROM tbl_TextbookIssues
WHERE ReturnDate IS NULL
ORDER BY IssueDate;";

        return await QueryIssuesAsync(sql, null, cancellationToken);
    }

    public async Task<IReadOnlyList<TextbookIssue>> GetIssuesByStudentAsync(int studentId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT IssueID, TextbookID, StudentID, IssueDate, ReturnDate
FROM tbl_TextbookIssues
WHERE StudentID = $studentId
ORDER BY IssueID DESC;";

        return await QueryIssuesAsync(sql, ("$studentId", studentId), cancellationToken);
    }

    public async Task<bool> HasActiveIssuesForTextbookAsync(int textbookId, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT COUNT(1) FROM tbl_TextbookIssues WHERE TextbookID = $textbookId AND ReturnDate IS NULL;";

        await using var connection = new SqliteConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("$textbookId", textbookId);

        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) > 0;
    }

    public async Task<int> CreateIssueAsync(TextbookIssue issue, CancellationToken cancellationToken = default)
    {
        const string sql = @"
INSERT INTO tbl_TextbookIssues (TextbookID, StudentID, IssueDate, ReturnDate)
VALUES ($textbookId, $studentId, $issueDate, NULL);
SELECT last_insert_rowid();";

        await using var connection = new SqliteConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = sql;
            command.Parameters.AddWithValue("$textbookId", issue.TextbookId);
            command.Parameters.AddWithValue("$studentId", issue.StudentId);
            command.Parameters.AddWithValue("$issueDate", FormatDate(issue.IssueDate));

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

    public async Task ReturnIssueAsync(int issueId, DateOnly returnDate, CancellationToken cancellationToken = default)
    {
        const string sql = @"
UPDATE tbl_TextbookIssues
SET ReturnDate = $returnDate
WHERE IssueID = $issueId AND ReturnDate IS NULL;";

        await using var connection = new SqliteConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = sql;
            command.Parameters.AddWithValue("$returnDate", FormatDate(returnDate));
            command.Parameters.AddWithValue("$issueId", issueId);

            var affected = await command.ExecuteNonQueryAsync(cancellationToken);
            if (affected == 0) throw new InvalidOperationException("Active issue record not found.");

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task SetAvailableCopiesAsync(int textbookId, int availableCopies, CancellationToken cancellationToken = default)
    {
        const string sql = @"
UPDATE tbl_Textbooks
SET AvailableCopies = $availableCopies
WHERE TextbookID = $textbookId;";

        await using var connection = new SqliteConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = sql;
            command.Parameters.AddWithValue("$availableCopies", availableCopies);
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

    private async Task<IReadOnlyList<TextbookIssue>> QueryIssuesAsync(
        string sql,
        (string Name, int Value)? parameter,
        CancellationToken cancellationToken)
    {
        await using var connection = new SqliteConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        if (parameter is not null)
        {
            command.Parameters.AddWithValue(parameter.Value.Name, parameter.Value.Value);
        }

        var result = new List<TextbookIssue>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new TextbookIssue
            {
                IssueId = reader.GetInt32(0),
                TextbookId = reader.GetInt32(1),
                StudentId = reader.GetInt32(2),
                IssueDate = ParseDate(reader.GetString(3)),
                ReturnDate = reader.IsDBNull(4) ? null : ParseDate(reader.GetString(4))
            });
        }

        return result;
    }

    private static Textbook MapTextbook(SqliteDataReader reader)
    {
        return new Textbook
        {
            TextbookId = reader.GetInt32(0),
            Title = reader.GetString(1),
            SubjectName = reader.IsDBNull(2) ? null : reader.GetString(2),
            GradeLevel = reader.IsDBNull(3) ? null : reader.GetString(3),
            TotalCopies = reader.GetInt32(4),
            AvailableCopies = reader.GetInt32(5)
        };
    }

    private static string FormatDate(DateOnly date) => date.ToString("yyyy-MM-dd");

    private static DateOnly ParseDate(string value) => DateOnly.ParseExact(value, "yyyy-MM-dd");
}
