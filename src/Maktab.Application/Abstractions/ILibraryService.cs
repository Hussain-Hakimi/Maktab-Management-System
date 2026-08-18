using Maktab.Domain.Entities;

namespace Maktab.Application.Abstractions;

public interface ILibraryService
{
    Task<IReadOnlyList<LibraryBook>> GetBooksAsync(CancellationToken cancellationToken = default);
    Task<int> AddBookAsync(string title, string author, string category, int totalCopies, CancellationToken cancellationToken = default);
    Task UpdateBookAsync(int bookId, string title, string author, string category, int totalCopies, CancellationToken cancellationToken = default);
    Task DeleteBookAsync(int bookId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BookLoanDto>> GetLoanHistoryAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BookLoanDto>> GetActiveLoansAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BookLoanDto>> GetOverdueLoansAsync(CancellationToken cancellationToken = default);
    Task<int> IssueBookAsync(int bookId, int studentId, int loanDays = 14, CancellationToken cancellationToken = default);
    Task ReturnBookAsync(int loanId, CancellationToken cancellationToken = default);
}
