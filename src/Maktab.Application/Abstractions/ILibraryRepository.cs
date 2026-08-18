using Maktab.Domain.Entities;

namespace Maktab.Application.Abstractions;

public interface ILibraryRepository
{
    Task<IReadOnlyList<LibraryBook>> GetBooksAsync(CancellationToken cancellationToken = default);
    Task<LibraryBook?> GetBookByIdAsync(int bookId, CancellationToken cancellationToken = default);
    Task<int> CreateBookAsync(LibraryBook book, CancellationToken cancellationToken = default);
    Task UpdateBookAsync(LibraryBook book, CancellationToken cancellationToken = default);
    Task DeleteBookAsync(int bookId, CancellationToken cancellationToken = default);
    Task AdjustAvailableCopiesAsync(int bookId, int delta, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BookLoan>> GetLoansAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BookLoan>> GetLoansByBookAsync(int bookId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BookLoan>> GetActiveLoansAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BookLoan>> GetOverdueLoansAsync(DateOnly today, CancellationToken cancellationToken = default);
    Task<BookLoan?> GetLoanByIdAsync(int loanId, CancellationToken cancellationToken = default);
    Task<int> CreateLoanAsync(BookLoan loan, CancellationToken cancellationToken = default);
    Task MarkLoanReturnedAsync(int loanId, DateOnly returnDate, CancellationToken cancellationToken = default);
}
