using Maktab.Application.Abstractions;
using Maktab.Application.Services;
using Maktab.Domain.Entities;
using Maktab.Domain.Enums;

namespace Maktab.Tests;

public class BookServiceTests
{
    private sealed class InMemoryBookRepository : IBookRepository
    {
        private readonly List<Book> _books = [];
        private readonly List<BookIssue> _issues = [];
        private int _nextBookId = 1;
        private int _nextIssueId = 1;

        public Task<IReadOnlyList<Book>> GetBooksAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Book>>(_books.ToList());

        public Task<Book?> GetBookByIdAsync(int bookId, CancellationToken cancellationToken = default)
            => Task.FromResult(_books.FirstOrDefault(b => b.BookId == bookId));

        public Task<int> CreateBookAsync(Book book, CancellationToken cancellationToken = default)
        {
            book.BookId = _nextBookId++;
            _books.Add(book);
            return Task.FromResult(book.BookId);
        }

        public Task UpdateBookAsync(Book book, CancellationToken cancellationToken = default)
        {
            var idx = _books.FindIndex(b => b.BookId == book.BookId);
            if (idx >= 0) _books[idx] = book;
            return Task.CompletedTask;
        }

        public Task DeleteBookAsync(int bookId, CancellationToken cancellationToken = default)
        {
            _books.RemoveAll(b => b.BookId == bookId);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<BookIssueDto>> GetIssuesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<BookIssueDto>>(_issues.Select(i => new BookIssueDto
            {
                IssueId = i.IssueId,
                BookId = i.BookId,
                StudentId = i.StudentId,
                IssueDate = i.IssueDate,
                DueDate = i.DueDate,
                ReturnDate = i.ReturnDate,
                Status = i.Status
            }).ToList());

        public Task<IReadOnlyList<BookIssueDto>> GetActiveIssuesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<BookIssueDto>>(_issues.Where(i => i.Status == BookIssueStatus.Issued).Select(i => new BookIssueDto
            {
                IssueId = i.IssueId,
                BookId = i.BookId,
                StudentId = i.StudentId,
                IssueDate = i.IssueDate,
                DueDate = i.DueDate,
                ReturnDate = i.ReturnDate,
                Status = i.Status
            }).ToList());

        public Task<IReadOnlyList<BookIssueDto>> GetOverdueIssuesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<BookIssueDto>>(_issues.Where(i => i.Status == BookIssueStatus.Issued && i.DueDate < DateTime.Today).Select(i => new BookIssueDto
            {
                IssueId = i.IssueId,
                BookId = i.BookId,
                StudentId = i.StudentId,
                IssueDate = i.IssueDate,
                DueDate = i.DueDate,
                ReturnDate = i.ReturnDate,
                Status = i.Status
            }).ToList());

        public Task<int> IssueBookAsync(BookIssue issue, CancellationToken cancellationToken = default)
        {
            issue.IssueId = _nextIssueId++;
            _issues.Add(issue);
            var book = _books.First(b => b.BookId == issue.BookId);
            book.AvailableCopies--;
            return Task.FromResult(issue.IssueId);
        }

        public Task ReturnBookAsync(int issueId, DateTime returnDate, CancellationToken cancellationToken = default)
        {
            var issue = _issues.First(i => i.IssueId == issueId);
            issue.ReturnDate = returnDate;
            issue.Status = BookIssueStatus.Returned;
            var book = _books.First(b => b.BookId == issue.BookId);
            book.AvailableCopies++;
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task AddBook_WithValidData_ReturnsIdAndCopiesAvailableEqualToTotal()
    {
        var repo = new InMemoryBookRepository();
        var service = new BookService(repo);

        var id = await service.AddBookAsync(new SaveBookDto("Clean Code", "Robert Martin", null, "Programming", 5));

        Assert.True(id > 0);
        var books = await service.GetBooksAsync();
        Assert.Single(books);
        Assert.Equal(5, books[0].TotalCopies);
        Assert.Equal(5, books[0].AvailableCopies);
    }

    [Fact]
    public async Task IssueBook_DecrementsAvailableCopies()
    {
        var repo = new InMemoryBookRepository();
        var service = new BookService(repo);
        var bookId = await service.AddBookAsync(new SaveBookDto("Clean Code", "Robert Martin", null, "Programming", 3));
        var studentId = 1; // Not validated here

        await service.IssueBookAsync(new IssueBookDto(bookId, studentId, DateTime.Today.AddDays(7)));

        var books = await service.GetBooksAsync();
        Assert.Equal(2, books[0].AvailableCopies);
    }

    [Fact]
    public async Task ReturnBook_IncrementsAvailableCopies()
    {
        var repo = new InMemoryBookRepository();
        var service = new BookService(repo);
        var bookId = await service.AddBookAsync(new SaveBookDto("Clean Code", "Robert Martin", null, "Programming", 2));
        var issueId = await service.IssueBookAsync(new IssueBookDto(bookId, 1, DateTime.Today.AddDays(7)));

        await service.ReturnBookAsync(new ReturnBookDto(issueId));

        var books = await service.GetBooksAsync();
        Assert.Equal(2, books[0].AvailableCopies);
        var issues = await service.GetIssuesAsync();
        Assert.Single(issues);
        Assert.Equal(BookIssueStatus.Returned, issues[0].Status);
    }

    [Fact]
    public async Task IssueBook_WhenNoAvailableCopies_ThrowsInvalidOperationException()
    {
        var repo = new InMemoryBookRepository();
        var service = new BookService(repo);
        var bookId = await service.AddBookAsync(new SaveBookDto("Clean Code", "Robert Martin", null, "Programming", 1));

        // Issue the only copy
        await service.IssueBookAsync(new IssueBookDto(bookId, 1, DateTime.Today.AddDays(7)));

        // Try to issue again
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await service.IssueBookAsync(new IssueBookDto(bookId, 2, DateTime.Today.AddDays(7)));
        });
    }
}
