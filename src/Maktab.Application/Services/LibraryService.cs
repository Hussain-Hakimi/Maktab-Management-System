using Maktab.Application.Abstractions;
using Maktab.Domain.Entities;

namespace Maktab.Application.Services;

public sealed class LibraryService(
    ILibraryRepository libraryRepository,
    IStudentRepository studentRepository) : ILibraryService
{
    public Task<IReadOnlyList<LibraryBook>> GetBooksAsync(CancellationToken cancellationToken = default)
    {
        return libraryRepository.GetBooksAsync(cancellationToken);
    }

    public Task<int> AddBookAsync(string title, string author, string category, int totalCopies, CancellationToken cancellationToken = default)
    {
        ValidateBook(title, totalCopies);

        var book = new LibraryBook
        {
            Title = title.Trim(),
            Author = author?.Trim() ?? string.Empty,
            Category = category?.Trim() ?? string.Empty,
            TotalCopies = totalCopies,
            AvailableCopies = totalCopies
        };

        return libraryRepository.CreateBookAsync(book, cancellationToken);
    }

    public async Task UpdateBookAsync(int bookId, string title, string author, string category, int totalCopies, CancellationToken cancellationToken = default)
    {
        if (bookId <= 0) throw new ArgumentOutOfRangeException(nameof(bookId));
        ValidateBook(title, totalCopies);

        var existing = await libraryRepository.GetBookByIdAsync(bookId, cancellationToken);
        if (existing is null) throw new InvalidOperationException("Book not found.");

        // Copies currently out on loan cannot be removed by shrinking the total.
        var loanedOut = existing.TotalCopies - existing.AvailableCopies;
        if (totalCopies < loanedOut)
        {
            throw new InvalidOperationException($"Cannot set total copies to {totalCopies}: {loanedOut} copies are currently issued to students.");
        }

        var book = new LibraryBook
        {
            BookId = bookId,
            Title = title.Trim(),
            Author = author?.Trim() ?? string.Empty,
            Category = category?.Trim() ?? string.Empty,
            TotalCopies = totalCopies,
            AvailableCopies = totalCopies - loanedOut
        };

        await libraryRepository.UpdateBookAsync(book, cancellationToken);
    }

    public async Task DeleteBookAsync(int bookId, CancellationToken cancellationToken = default)
    {
        if (bookId <= 0) throw new ArgumentOutOfRangeException(nameof(bookId));

        var loans = await libraryRepository.GetLoansByBookAsync(bookId, cancellationToken);
        if (loans.Count > 0)
        {
            throw new InvalidOperationException("This book has loan records and cannot be deleted. Return all copies and keep the record for history.");
        }

        await libraryRepository.DeleteBookAsync(bookId, cancellationToken);
    }

    public async Task<IReadOnlyList<BookLoanDto>> GetLoanHistoryAsync(CancellationToken cancellationToken = default)
    {
        var loans = await libraryRepository.GetLoansAsync(cancellationToken);
        return await ToDtosAsync(loans, cancellationToken);
    }

    public async Task<IReadOnlyList<BookLoanDto>> GetActiveLoansAsync(CancellationToken cancellationToken = default)
    {
        var loans = await libraryRepository.GetActiveLoansAsync(cancellationToken);
        return await ToDtosAsync(loans, cancellationToken);
    }

    public async Task<IReadOnlyList<BookLoanDto>> GetOverdueLoansAsync(CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var loans = await libraryRepository.GetOverdueLoansAsync(today, cancellationToken);
        return await ToDtosAsync(loans, cancellationToken);
    }

    public async Task<int> IssueBookAsync(int bookId, int studentId, int loanDays = 14, CancellationToken cancellationToken = default)
    {
        if (bookId <= 0) throw new ArgumentOutOfRangeException(nameof(bookId));
        if (studentId <= 0) throw new ArgumentOutOfRangeException(nameof(studentId));
        if (loanDays < 1) throw new ArgumentOutOfRangeException(nameof(loanDays), "Loan period must be at least one day.");

        var book = await libraryRepository.GetBookByIdAsync(bookId, cancellationToken);
        if (book is null) throw new InvalidOperationException("Book not found.");
        if (book.AvailableCopies <= 0)
        {
            throw new InvalidOperationException($"No copies of '{book.Title}' are available right now.");
        }

        var student = await studentRepository.GetStudentByIdAsync(studentId, cancellationToken);
        if (student is null) throw new InvalidOperationException("Student not found.");

        var today = DateOnly.FromDateTime(DateTime.Today);
        var loan = new BookLoan
        {
            BookId = bookId,
            StudentId = studentId,
            IssueDate = today,
            DueDate = today.AddDays(loanDays)
        };

        var loanId = await libraryRepository.CreateLoanAsync(loan, cancellationToken);
        await libraryRepository.AdjustAvailableCopiesAsync(bookId, -1, cancellationToken);
        return loanId;
    }

    public async Task ReturnBookAsync(int loanId, CancellationToken cancellationToken = default)
    {
        if (loanId <= 0) throw new ArgumentOutOfRangeException(nameof(loanId));

        var loan = await libraryRepository.GetLoanByIdAsync(loanId, cancellationToken);
        if (loan is null) throw new InvalidOperationException("Loan record not found.");
        if (loan.IsReturned) throw new InvalidOperationException("This book has already been returned.");

        var today = DateOnly.FromDateTime(DateTime.Today);
        await libraryRepository.MarkLoanReturnedAsync(loanId, today, cancellationToken);
        await libraryRepository.AdjustAvailableCopiesAsync(loan.BookId, +1, cancellationToken);
    }

    private async Task<IReadOnlyList<BookLoanDto>> ToDtosAsync(IReadOnlyList<BookLoan> loans, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var books = await libraryRepository.GetBooksAsync(cancellationToken);
        var bookMap = books.ToDictionary(b => b.BookId, b => b.Title);
        var students = await studentRepository.GetStudentsAsync(cancellationToken);
        var studentMap = students.ToDictionary(s => s.StudentId);

        return loans.Select(l =>
        {
            studentMap.TryGetValue(l.StudentId, out var student);
            return new BookLoanDto
            {
                LoanId = l.LoanId,
                BookId = l.BookId,
                BookTitle = bookMap.TryGetValue(l.BookId, out var title) ? title : $"Book {l.BookId}",
                StudentId = l.StudentId,
                StudentName = student is null ? $"شاگرد {l.StudentId}" : $"{student.FirstName} {student.LastName}",
                RollNumber = student?.RollNumber ?? string.Empty,
                IssueDate = l.IssueDate,
                DueDate = l.DueDate,
                ReturnDate = l.ReturnDate,
                IsOverdue = l.IsOverdue(today)
            };
        }).ToList();
    }

    private static void ValidateBook(string title, int totalCopies)
    {
        if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Book title is required.", nameof(title));
        if (totalCopies < 1) throw new ArgumentOutOfRangeException(nameof(totalCopies), "Total copies must be at least 1.");
    }
}
