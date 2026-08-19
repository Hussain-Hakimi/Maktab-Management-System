namespace Maktab.Application.Abstractions;

public interface IBookService
{
    Task<IReadOnlyList<BookDto>> GetBooksAsync(CancellationToken cancellationToken = default);
    Task<int> AddBookAsync(SaveBookDto book, CancellationToken cancellationToken = default);
    Task UpdateBookAsync(int bookId, SaveBookDto book, CancellationToken cancellationToken = default);
    Task DeleteBookAsync(int bookId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BookIssueDto>> GetIssuesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BookIssueDto>> GetOverdueIssuesAsync(CancellationToken cancellationToken = default);

    Task<int> IssueBookAsync(IssueBookDto issue, CancellationToken cancellationToken = default);
    Task ReturnBookAsync(ReturnBookDto returnInfo, CancellationToken cancellationToken = default);
}
