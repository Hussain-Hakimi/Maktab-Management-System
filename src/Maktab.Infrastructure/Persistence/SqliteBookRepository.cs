using Microsoft.Data.Sqlite;
using Maktab.Application.Abstractions;
using Maktab.Domain.Entities;
using Maktab.Domain.Enums;

namespace Maktab.Infrastructure.Persistence;

public sealed class SqliteBookRepository(IConnectionStringProvider connectionStringProvider) : IBookRepository
{
    public async Task<IReadOnlyList<Book>> GetBooksAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT BookID, Title, Author, ISBN, Category, TotalCopies, AvailableCopies
FROM tbl_Books
ORDER BY Title;";

        await using var connection = new SqliteConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        var result = new List<Book>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(MapBook(reader));
        }

        return result;
    }

    public async Task<Book?> GetBookByIdAsync(int bookId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT BookID, Title, Author, ISBN, Category, TotalCopies, AvailableCopies
FROM tbl_Books
WHERE BookID = $bookId;";

        await using var connection = new SqliteConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("$bookId", bookId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return MapBook(reader);
        }

        return null;
    }

    public async Task<int> CreateBookAsync(Book book, CancellationToken cancellationToken = default)
    {
        const string sql = @"
INSERT INTO tbl_Books (Title, Author, ISBN, Category, TotalCopies, AvailableCopies)
VALUES ($title, $author, $isbn, $category, $totalCopies, $availableCopies);
SELECT last_insert_rowid();";

        await using var connection = new SqliteConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = sql;
            command.Parameters.AddWithValue("$title", book.Title);
            command.Parameters.AddWithValue("$author", book.Author);
            command.Parameters.AddWithValue("$isbn", (object?)book.ISBN ?? DBNull.Value);
            command.Parameters.AddWithValue("$category", (object?)book.Category ?? DBNull.Value);
            command.Parameters.AddWithValue("$totalCopies", book.TotalCopies);
            command.Parameters.AddWithValue("$availableCopies", book.AvailableCopies);

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

    public async Task UpdateBookAsync(Book book, CancellationToken cancellationToken = default)
    {
        const string sql = @"
UPDATE tbl_Books
SET Title = $title,
    Author = $author,
    ISBN = $isbn,
    Category = $category,
    TotalCopies = $totalCopies,
    AvailableCopies = $availableCopies
WHERE BookID = $bookId;";

        await using var connection = new SqliteConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = sql;
            command.Parameters.AddWithValue("$title", book.Title);
            command.Parameters.AddWithValue("$author", book.Author);
            command.Parameters.AddWithValue("$isbn", (object?)book.ISBN ?? DBNull.Value);
            command.Parameters.AddWithValue("$category", (object?)book.Category ?? DBNull.Value);
            command.Parameters.AddWithValue("$totalCopies", book.TotalCopies);
            command.Parameters.AddWithValue("$availableCopies", book.AvailableCopies);
            command.Parameters.AddWithValue("$bookId", book.BookId);

            var affected = await command.ExecuteNonQueryAsync(cancellationToken);
            if (affected == 0) throw new InvalidOperationException("Book not found.");

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task DeleteBookAsync(int bookId, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            // 1. Check if book has borrowing issues
            await using (var checkIssuesCmd = connection.CreateCommand())
            {
                checkIssuesCmd.Transaction = transaction;
                checkIssuesCmd.CommandText = "SELECT COUNT(1) FROM tbl_BookIssues WHERE BookID = $bookId;";
                checkIssuesCmd.Parameters.AddWithValue("$bookId", bookId);
                var issuesCount = Convert.ToInt32(await checkIssuesCmd.ExecuteScalarAsync(cancellationToken));
                if (issuesCount > 0)
                {
                    throw new InvalidOperationException("این کتاب دارای سوابق امانت‌دهی در سیستم است و قابل حذف نیست. ابتدا باید سوابق امانت‌دهی آن را بررسی یا حذف کنید.");
                }
            }

            // 2. Delete book
            const string sql = "DELETE FROM tbl_Books WHERE BookID = $bookId;";
            await using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = sql;
                command.Parameters.AddWithValue("$bookId", bookId);

                var affected = await command.ExecuteNonQueryAsync(cancellationToken);
                if (affected == 0) throw new InvalidOperationException("کتاب مورد نظر یافت نشد.");
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<IReadOnlyList<BookIssueDto>> GetIssuesAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT i.IssueID, i.BookID, b.Title, i.StudentID, s.FirstName || ' ' || s.LastName AS StudentName, s.RollNumber,
       i.IssueDate, i.DueDate, i.ReturnDate, i.Status
FROM tbl_BookIssues i
JOIN tbl_Books b ON i.BookID = b.BookID
JOIN tbl_Students s ON i.StudentID = s.StudentID
ORDER BY i.IssueDate DESC;";

        await using var connection = new SqliteConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        var result = new List<BookIssueDto>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(MapIssue(reader));
        }

        return result;
    }

    public async Task<IReadOnlyList<BookIssueDto>> GetActiveIssuesAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT i.IssueID, i.BookID, b.Title, i.StudentID, s.FirstName || ' ' || s.LastName AS StudentName, s.RollNumber,
       i.IssueDate, i.DueDate, i.ReturnDate, i.Status
FROM tbl_BookIssues i
JOIN tbl_Books b ON i.BookID = b.BookID
JOIN tbl_Students s ON i.StudentID = s.StudentID
WHERE i.Status = 'Issued'
ORDER BY i.DueDate;";

        await using var connection = new SqliteConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        var result = new List<BookIssueDto>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(MapIssue(reader));
        }

        return result;
    }

    public async Task<IReadOnlyList<BookIssueDto>> GetOverdueIssuesAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT i.IssueID, i.BookID, b.Title, i.StudentID, s.FirstName || ' ' || s.LastName AS StudentName, s.RollNumber,
       i.IssueDate, i.DueDate, i.ReturnDate, i.Status
FROM tbl_BookIssues i
JOIN tbl_Books b ON i.BookID = b.BookID
JOIN tbl_Students s ON i.StudentID = s.StudentID
WHERE i.Status = 'Issued' AND i.DueDate < $today
ORDER BY i.DueDate;";

        await using var connection = new SqliteConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("$today", DateTime.Today.ToString("yyyy-MM-dd"));

        var result = new List<BookIssueDto>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(MapIssue(reader));
        }

        return result;
    }

    public async Task<int> IssueBookAsync(BookIssue issue, CancellationToken cancellationToken = default)
    {
        const string insertIssueSql = @"
INSERT INTO tbl_BookIssues (BookID, StudentID, IssueDate, DueDate, Status)
VALUES ($bookId, $studentId, $issueDate, $dueDate, 'Issued');
SELECT last_insert_rowid();";

        const string decrementAvailableSql = @"
UPDATE tbl_Books
SET AvailableCopies = AvailableCopies - 1
WHERE BookID = $bookId AND AvailableCopies > 0;
SELECT changes();";

        await using var connection = new SqliteConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            // Insert issue record
            await using (var insertCmd = connection.CreateCommand())
            {
                insertCmd.Transaction = transaction;
                insertCmd.CommandText = insertIssueSql;
                insertCmd.Parameters.AddWithValue("$bookId", issue.BookId);
                insertCmd.Parameters.AddWithValue("$studentId", issue.StudentId);
                insertCmd.Parameters.AddWithValue("$issueDate", issue.IssueDate.ToString("yyyy-MM-dd"));
                insertCmd.Parameters.AddWithValue("$dueDate", issue.DueDate.ToString("yyyy-MM-dd"));
                var issueId = Convert.ToInt32(await insertCmd.ExecuteScalarAsync(cancellationToken));

                // Decrement available copies
                await using var updateCmd = connection.CreateCommand();
                updateCmd.Transaction = transaction;
                updateCmd.CommandText = decrementAvailableSql;
                updateCmd.Parameters.AddWithValue("$bookId", issue.BookId);
                var changes = Convert.ToInt32(await updateCmd.ExecuteScalarAsync(cancellationToken));
                if (changes == 0) throw new InvalidOperationException("No available copies.");

                await transaction.CommitAsync(cancellationToken);
                return issueId;
            }
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task ReturnBookAsync(int issueId, DateTime returnDate, CancellationToken cancellationToken = default)
    {
        const string updateIssueSql = @"
UPDATE tbl_BookIssues
SET ReturnDate = $returnDate,
    Status = 'Returned'
WHERE IssueID = $issueId AND Status = 'Issued';
SELECT changes();";

        const string incrementAvailableSql = @"
UPDATE tbl_Books
SET AvailableCopies = AvailableCopies + 1
WHERE BookID = (SELECT BookID FROM tbl_BookIssues WHERE IssueID = $issueId);";

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

    private static Book MapBook(SqliteDataReader reader)
    {
        return new Book
        {
            BookId = reader.GetInt32(0),
            Title = reader.GetString(1),
            Author = reader.GetString(2),
            ISBN = reader.IsDBNull(3) ? null : reader.GetString(3),
            Category = reader.IsDBNull(4) ? null : reader.GetString(4),
            TotalCopies = reader.GetInt32(5),
            AvailableCopies = reader.GetInt32(6)
        };
    }

    private static BookIssueDto MapIssue(SqliteDataReader reader)
    {
        return new BookIssueDto
        {
            IssueId = reader.GetInt32(0),
            BookId = reader.GetInt32(1),
            BookTitle = reader.GetString(2),
            StudentId = reader.GetInt32(3),
            StudentName = reader.GetString(4),
            RollNumber = reader.GetString(5),
            IssueDate = DateTime.Parse(reader.GetString(6)),
            DueDate = DateTime.Parse(reader.GetString(7)),
            ReturnDate = reader.IsDBNull(8) ? null : DateTime.Parse(reader.GetString(8)),
            Status = reader.GetString(9) == "Returned" ? BookIssueStatus.Returned : BookIssueStatus.Issued
        };
    }
}
