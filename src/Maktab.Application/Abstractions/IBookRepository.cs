using Maktab.Domain.Entities;

namespace Maktab.Application.Abstractions;

public interface IBookRepository
{
    Task<IReadOnlyList<Book>> GetBooksAsync(CancellationToken cancellationToken = default);
    Task<Book?> GetBookByIdAsync(int bookId, CancellationToken cancellationToken = default);
    Task<int> CreateBookAsync(Book book, CancellationToken cancellationToken = default);
    Task UpdateBookAsync(Book book, CancellationToken cancellationToken = default);
    Task DeleteBookAsync(int bookId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BookIssueDto>> GetIssuesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BookIssueDto>> GetActiveIssuesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BookIssueDto>> GetOverdueIssuesAsync(CancellationToken cancellationToken = default);

    Task<int> IssueBookAsync(BookIssue issue, CancellationToken cancellationToken = default);
    Task ReturnBookAsync(int issueId, DateTime returnDate, CancellationToken cancellationToken = default);
}
