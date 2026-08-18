using Microsoft.Data.Sqlite;
using Maktab.Application.Abstractions;
using Maktab.Domain.Entities;

namespace Maktab.Infrastructure.Persistence;

public sealed class SqliteLibraryRepository(IConnectionStringProvider connectionStringProvider) : ILibraryRepository
{
    public async Task<IReadOnlyList<LibraryBook>> GetBooksAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT BookID, Title, Author, Category, TotalCopies, AvailableCopies
FROM tbl_LibraryBooks
ORDER BY Title;";

        await using var connection = new SqliteConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        var result = new List<LibraryBook>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(MapBook(reader));
        }

        return result;
    }

    public async Task<LibraryBook?> GetBookByIdAsync(int bookId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT BookID, Title, Author, Category, TotalCopies, AvailableCopies
FROM tbl_LibraryBooks
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

    public async Task<int> CreateBookAsync(LibraryBook book, CancellationToken cancellationToken = default)
    {
        const string sql = @"
INSERT INTO tbl_LibraryBooks (Title, Author, Category, TotalCopies, AvailableCopies)
VALUES ($title, $author, $category, $totalCopies, $availableCopies);
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

    public async Task UpdateBookAsync(LibraryBook book, CancellationToken cancellationToken = default)
    {
        const string sql = @"
UPDATE tbl_LibraryBooks
SET Title = $title,
    Author = $author,
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
        const string sql = "DELETE FROM tbl_LibraryBooks WHERE BookID = $bookId;";

        await using var connection = new SqliteConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = sql;
            command.Parameters.AddWithValue("$bookId", bookId);

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

    public async Task<IReadOnlyList<BookLoan>> GetLoansAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT LoanID, BookID, StudentID, IssueDate, DueDate, ReturnDate
FROM tbl_BookLoans
ORDER BY LoanID DESC;";

        return await QueryLoansAsync(sql, null, cancellationToken);
    }

    public async Task<IReadOnlyList<BookLoan>> GetActiveLoansAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT LoanID, BookID, StudentID, IssueDate, DueDate, ReturnDate
FROM tbl_BookLoans
WHERE ReturnDate IS NULL
ORDER BY DueDate;";

        return await QueryLoansAsync(sql, null, cancellationToken);
    }

    public async Task<IReadOnlyList<BookLoan>> GetLoansByStudentAsync(int studentId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT LoanID, BookID, StudentID, IssueDate, DueDate, ReturnDate
FROM tbl_BookLoans
WHERE StudentID = $studentId
ORDER BY LoanID DESC;";

        return await QueryLoansAsync(sql, ("$studentId", studentId), cancellationToken);
    }

    public async Task<bool> HasActiveLoansForBookAsync(int bookId, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT COUNT(1) FROM tbl_BookLoans WHERE BookID = $bookId AND ReturnDate IS NULL;";

        await using var connection = new SqliteConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("$bookId", bookId);

        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) > 0;
    }

    public async Task<int> CreateLoanAsync(BookLoan loan, CancellationToken cancellationToken = default)
    {
        const string sql = @"
INSERT INTO tbl_BookLoans (BookID, StudentID, IssueDate, DueDate, ReturnDate)
VALUES ($bookId, $studentId, $issueDate, $dueDate, NULL);
SELECT last_insert_rowid();";

        await using var connection = new SqliteConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = sql;
            command.Parameters.AddWithValue("$bookId", loan.BookId);
            command.Parameters.AddWithValue("$studentId", loan.StudentId);
            command.Parameters.AddWithValue("$issueDate", FormatDate(loan.IssueDate));
            command.Parameters.AddWithValue("$dueDate", FormatDate(loan.DueDate));

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

    public async Task ReturnLoanAsync(int loanId, DateOnly returnDate, CancellationToken cancellationToken = default)
    {
        const string sql = @"
UPDATE tbl_BookLoans
SET ReturnDate = $returnDate
WHERE LoanID = $loanId AND ReturnDate IS NULL;";

        await using var connection = new SqliteConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = sql;
            command.Parameters.AddWithValue("$returnDate", FormatDate(returnDate));
            command.Parameters.AddWithValue("$loanId", loanId);

            var affected = await command.ExecuteNonQueryAsync(cancellationToken);
            if (affected == 0) throw new InvalidOperationException("Active loan not found.");

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task SetAvailableCopiesAsync(int bookId, int availableCopies, CancellationToken cancellationToken = default)
    {
        const string sql = @"
UPDATE tbl_LibraryBooks
SET AvailableCopies = $availableCopies
WHERE BookID = $bookId;";

        await using var connection = new SqliteConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = sql;
            command.Parameters.AddWithValue("$availableCopies", availableCopies);
            command.Parameters.AddWithValue("$bookId", bookId);

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

    private async Task<IReadOnlyList<BookLoan>> QueryLoansAsync(
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

        var result = new List<BookLoan>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new BookLoan
            {
                LoanId = reader.GetInt32(0),
                BookId = reader.GetInt32(1),
                StudentId = reader.GetInt32(2),
                IssueDate = ParseDate(reader.GetString(3)),
                DueDate = ParseDate(reader.GetString(4)),
                ReturnDate = reader.IsDBNull(5) ? null : ParseDate(reader.GetString(5))
            });
        }

        return result;
    }

    private static LibraryBook MapBook(SqliteDataReader reader)
    {
        return new LibraryBook
        {
            BookId = reader.GetInt32(0),
            Title = reader.GetString(1),
            Author = reader.GetString(2),
            Category = reader.IsDBNull(3) ? null : reader.GetString(3),
            TotalCopies = reader.GetInt32(4),
            AvailableCopies = reader.GetInt32(5)
        };
    }

    private static string FormatDate(DateOnly date) => date.ToString("yyyy-MM-dd");

    private static DateOnly ParseDate(string value) => DateOnly.ParseExact(value, "yyyy-MM-dd");
}
