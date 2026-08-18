using Maktab.Domain.Entities;

namespace Maktab.Application.Abstractions;

public interface ILibraryRepository
{
    Task<IReadOnlyList<LibraryBook>> GetBooksAsync(CancellationToken cancellationToken = default);
    Task<LibraryBook?> GetBookByIdAsync(int bookId, CancellationToken cancellationToken = default);
    Task<int> CreateBookAsync(LibraryBook book, CancellationToken cancellationToken = default);
    Task UpdateBookAsync(LibraryBook book, CancellationToken cancellationToken = default);
    Task DeleteBookAsync(int bookId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BookLoan>> GetLoansAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BookLoan>> GetActiveLoansAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BookLoan>> GetLoansByStudentAsync(int studentId, CancellationToken cancellationToken = default);
    Task<bool> HasActiveLoansForBookAsync(int bookId, CancellationToken cancellationToken = default);
    Task<int> CreateLoanAsync(BookLoan loan, CancellationToken cancellationToken = default);
    Task ReturnLoanAsync(int loanId, DateOnly returnDate, CancellationToken cancellationToken = default);
    Task SetAvailableCopiesAsync(int bookId, int availableCopies, CancellationToken cancellationToken = default);
}
