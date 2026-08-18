using Maktab.Application.Abstractions;
using Maktab.Domain.Entities;

namespace Maktab.Application.Services;

public sealed class LibraryService(
    ILibraryRepository libraryRepository,
    IStudentRepository studentRepository) : ILibraryService
{
    public const int DefaultLoanDays = 14;

    public Task<IReadOnlyList<LibraryBook>> GetAllBooksAsync(CancellationToken cancellationToken = default)
    {
        return libraryRepository.GetBooksAsync(cancellationToken);
    }

    public Task<int> AddBookAsync(string title, string author, string? category, int totalCopies, CancellationToken cancellationToken = default)
    {
        ValidateBook(title, totalCopies);

        var book = new LibraryBook
        {
            Title = title.Trim(),
            Author = author?.Trim() ?? string.Empty,
            Category = string.IsNullOrWhiteSpace(category) ? null : category.Trim(),
            TotalCopies = totalCopies,
            AvailableCopies = totalCopies
        };

        return libraryRepository.CreateBookAsync(book, cancellationToken);
    }

    public async Task UpdateBookAsync(int bookId, string title, string author, string? category, int totalCopies, CancellationToken cancellationToken = default)
    {
        if (bookId <= 0) throw new ArgumentOutOfRangeException(nameof(bookId));
        ValidateBook(title, totalCopies);

        var existing = await libraryRepository.GetBookByIdAsync(bookId, cancellationToken);
        if (existing is null) throw new InvalidOperationException("کتاب یافت نشد.");

        var issuedCopies = existing.TotalCopies - existing.AvailableCopies;
        if (totalCopies < issuedCopies)
        {
            throw new InvalidOperationException($"تعداد کل نسخه‌ها نمی‌تواند از تعداد نسخه‌های امانت‌داده‌شده ({issuedCopies}) کمتر باشد.");
        }

        existing.Title = title.Trim();
        existing.Author = author?.Trim() ?? string.Empty;
        existing.Category = string.IsNullOrWhiteSpace(category) ? null : category.Trim();
        existing.TotalCopies = totalCopies;
        existing.AvailableCopies = totalCopies - issuedCopies;

        await libraryRepository.UpdateBookAsync(existing, cancellationToken);
    }

    public async Task RemoveBookAsync(int bookId, CancellationToken cancellationToken = default)
    {
        if (bookId <= 0) throw new ArgumentOutOfRangeException(nameof(bookId));

        if (await libraryRepository.HasActiveLoansForBookAsync(bookId, cancellationToken))
        {
            throw new InvalidOperationException("این کتاب نسخه‌های امانت‌داده‌شده دارد و تا بازگشت آنها قابل حذف نیست.");
        }

        await libraryRepository.DeleteBookAsync(bookId, cancellationToken);
    }

    public async Task<IReadOnlyList<BookLoanDto>> GetAllLoansAsync(CancellationToken cancellationToken = default)
    {
        return await EnrichLoansAsync(await libraryRepository.GetLoansAsync(cancellationToken), cancellationToken);
    }

    public async Task<IReadOnlyList<BookLoanDto>> GetActiveLoansAsync(CancellationToken cancellationToken = default)
    {
        return await EnrichLoansAsync(await libraryRepository.GetActiveLoansAsync(cancellationToken), cancellationToken);
    }

    public async Task<IReadOnlyList<BookLoanDto>> GetOverdueLoansAsync(CancellationToken cancellationToken = default)
    {
        var active = await GetActiveLoansAsync(cancellationToken);
        return active.Where(l => l.IsOverdue).ToList();
    }

    public async Task<int> IssueBookAsync(int bookId, int studentId, int loanDays, CancellationToken cancellationToken = default)
    {
        if (bookId <= 0) throw new ArgumentOutOfRangeException(nameof(bookId));
        if (studentId <= 0) throw new ArgumentOutOfRangeException(nameof(studentId));
        if (loanDays < 1 || loanDays > 365) throw new ArgumentOutOfRangeException(nameof(loanDays), "مدت امانت باید بین ۱ و ۳۶۵ روز باشد.");

        var book = await libraryRepository.GetBookByIdAsync(bookId, cancellationToken);
        if (book is null) throw new InvalidOperationException("کتاب یافت نشد.");
        if (book.AvailableCopies <= 0)
        {
            throw new InvalidOperationException($"نسخه‌ای از کتاب «{book.Title}» در کتابخانه موجود نیست.");
        }

        var student = await studentRepository.GetStudentByIdAsync(studentId, cancellationToken);
        if (student is null) throw new InvalidOperationException($"شاگرد با آیدی {studentId} یافت نشد.");

        var today = DateOnly.FromDateTime(DateTime.Now);
        var loan = new BookLoan
        {
            BookId = bookId,
            StudentId = studentId,
            IssueDate = today,
            DueDate = today.AddDays(loanDays),
            ReturnDate = null
        };

        var loanId = await libraryRepository.CreateLoanAsync(loan, cancellationToken);
        await libraryRepository.SetAvailableCopiesAsync(bookId, book.AvailableCopies - 1, cancellationToken);
        return loanId;
    }

    public async Task ReturnBookAsync(int loanId, CancellationToken cancellationToken = default)
    {
        if (loanId <= 0) throw new ArgumentOutOfRangeException(nameof(loanId));

        var loans = await libraryRepository.GetLoansAsync(cancellationToken);
        var loan = loans.FirstOrDefault(l => l.LoanId == loanId);
        if (loan is null) throw new InvalidOperationException("رکورد امانت یافت نشد.");
        if (loan.IsReturned) throw new InvalidOperationException("این کتاب قبلاً بازگردانده شده است.");

        var today = DateOnly.FromDateTime(DateTime.Now);
        await libraryRepository.ReturnLoanAsync(loanId, today, cancellationToken);

        var book = await libraryRepository.GetBookByIdAsync(loan.BookId, cancellationToken);
        if (book is not null)
        {
            await libraryRepository.SetAvailableCopiesAsync(book.BookId, Math.Min(book.AvailableCopies + 1, book.TotalCopies), cancellationToken);
        }
    }

    private async Task<IReadOnlyList<BookLoanDto>> EnrichLoansAsync(IReadOnlyList<BookLoan> loans, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var result = new List<BookLoanDto>();

        foreach (var loan in loans)
        {
            var book = await libraryRepository.GetBookByIdAsync(loan.BookId, cancellationToken);
            var student = await studentRepository.GetStudentByIdAsync(loan.StudentId, cancellationToken);

            result.Add(new BookLoanDto
            {
                LoanId = loan.LoanId,
                BookId = loan.BookId,
                BookTitle = book?.Title ?? $"کتاب {loan.BookId}",
                StudentId = loan.StudentId,
                StudentName = student is null ? $"شاگرد {loan.StudentId}" : $"{student.FirstName} {student.LastName}",
                RollNumber = student?.RollNumber ?? string.Empty,
                IssueDate = loan.IssueDate,
                DueDate = loan.DueDate,
                ReturnDate = loan.ReturnDate,
                IsReturned = loan.IsReturned,
                IsOverdue = loan.IsOverdue(today)
            });
        }

        return result;
    }

    private static void ValidateBook(string title, int totalCopies)
    {
        if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("عنوان کتاب ضروری است.", nameof(title));
        if (totalCopies < 0) throw new ArgumentOutOfRangeException(nameof(totalCopies), "تعداد نسخه‌ها نمی‌تواند منفی باشد.");
    }
}
