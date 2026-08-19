using Maktab.Application.Abstractions;
using Maktab.Domain.Entities;
using Maktab.Domain.Enums;

namespace Maktab.Application.Services;

public sealed class BookService(IBookRepository repository) : IBookService
{
    public async Task<IReadOnlyList<BookDto>> GetBooksAsync(CancellationToken cancellationToken = default)
    {
        var books = await repository.GetBooksAsync(cancellationToken);
        return books.Select(b => new BookDto
        {
            BookId = b.BookId,
            Title = b.Title,
            Author = b.Author,
            ISBN = b.ISBN,
            Category = b.Category,
            TotalCopies = b.TotalCopies,
            AvailableCopies = b.AvailableCopies
        }).ToList();
    }

    public async Task<int> AddBookAsync(SaveBookDto book, CancellationToken cancellationToken = default)
    {
        ValidateBook(book);

        var entity = new Book
        {
            Title = book.Title.Trim(),
            Author = book.Author.Trim(),
            ISBN = string.IsNullOrWhiteSpace(book.ISBN) ? null : book.ISBN.Trim(),
            Category = string.IsNullOrWhiteSpace(book.Category) ? null : book.Category.Trim(),
            TotalCopies = book.TotalCopies,
            AvailableCopies = book.TotalCopies
        };

        return await repository.CreateBookAsync(entity, cancellationToken);
    }

    public async Task UpdateBookAsync(int bookId, SaveBookDto book, CancellationToken cancellationToken = default)
    {
        if (bookId <= 0) throw new ArgumentOutOfRangeException(nameof(bookId));
        ValidateBook(book);

        var existing = await repository.GetBookByIdAsync(bookId, cancellationToken);
        if (existing is null) throw new InvalidOperationException("Book not found.");

        // Ensure new total copies is not less than currently issued copies (total - available)
        var issuedCopies = existing.TotalCopies - existing.AvailableCopies;
        if (book.TotalCopies < issuedCopies)
        {
            throw new InvalidOperationException($"Total copies cannot be less than currently issued copies ({issuedCopies}).");
        }

        var entity = new Book
        {
            BookId = bookId,
            Title = book.Title.Trim(),
            Author = book.Author.Trim(),
            ISBN = string.IsNullOrWhiteSpace(book.ISBN) ? null : book.ISBN.Trim(),
            Category = string.IsNullOrWhiteSpace(book.Category) ? null : book.Category.Trim(),
            TotalCopies = book.TotalCopies,
            AvailableCopies = book.TotalCopies - issuedCopies
        };

        await repository.UpdateBookAsync(entity, cancellationToken);
    }

    public async Task DeleteBookAsync(int bookId, CancellationToken cancellationToken = default)
    {
        if (bookId <= 0) throw new ArgumentOutOfRangeException(nameof(bookId));

        var book = await repository.GetBookByIdAsync(bookId, cancellationToken);
        if (book is null) throw new InvalidOperationException("Book not found.");

        if (book.AvailableCopies != book.TotalCopies)
        {
            throw new InvalidOperationException("Cannot delete a book with active issues.");
        }

        await repository.DeleteBookAsync(bookId, cancellationToken);
    }

    public Task<IReadOnlyList<BookIssueDto>> GetIssuesAsync(CancellationToken cancellationToken = default)
    {
        return repository.GetIssuesAsync(cancellationToken);
    }

    public Task<IReadOnlyList<BookIssueDto>> GetOverdueIssuesAsync(CancellationToken cancellationToken = default)
    {
        return repository.GetOverdueIssuesAsync(cancellationToken);
    }

    public async Task<int> IssueBookAsync(IssueBookDto issue, CancellationToken cancellationToken = default)
    {
        if (issue.BookId <= 0) throw new ArgumentOutOfRangeException(nameof(issue.BookId));
        if (issue.StudentId <= 0) throw new ArgumentOutOfRangeException(nameof(issue.StudentId));
        if (issue.DueDate <= DateTime.Today) throw new ArgumentException("Due date must be in the future.");

        var book = await repository.GetBookByIdAsync(issue.BookId, cancellationToken);
        if (book is null) throw new InvalidOperationException("Book not found.");

        if (book.AvailableCopies <= 0) throw new InvalidOperationException("No available copies.");

        var entity = new BookIssue
        {
            BookId = issue.BookId,
            StudentId = issue.StudentId,
            IssueDate = DateTime.Today,
            DueDate = issue.DueDate.Date,
            Status = BookIssueStatus.Issued
        };

        return await repository.IssueBookAsync(entity, cancellationToken);
    }

    public async Task ReturnBookAsync(ReturnBookDto returnInfo, CancellationToken cancellationToken = default)
    {
        if (returnInfo.IssueId <= 0) throw new ArgumentOutOfRangeException(nameof(returnInfo.IssueId));
        await repository.ReturnBookAsync(returnInfo.IssueId, DateTime.Today, cancellationToken);
    }

    private static void ValidateBook(SaveBookDto book)
    {
        if (string.IsNullOrWhiteSpace(book.Title)) throw new ArgumentException("Title is required.");
        if (string.IsNullOrWhiteSpace(book.Author)) throw new ArgumentException("Author is required.");
        if (book.TotalCopies <= 0) throw new ArgumentOutOfRangeException(nameof(book.TotalCopies), "Total copies must be greater than zero.");
    }
}
