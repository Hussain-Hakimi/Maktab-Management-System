using Microsoft.Data.Sqlite;
using Maktab.Application.Abstractions;
using Maktab.Domain.Entities;
using Maktab.Domain.Enums;
using Maktab.Infrastructure.Persistence;

namespace Maktab.Tests;

public class SqliteBookRepositoryIntegrationTests : IDisposable
{
    private readonly string _tempDir;
    private readonly AppFolders _folders;
    private readonly ConnectionStringProvider _connectionStringProvider;
    private readonly SqliteBookRepository _bookRepository;
    private readonly SqliteStudentRepository _studentRepository;
    private readonly SqliteClassSubjectRepository _classSubjectRepository;

    public SqliteBookRepositoryIntegrationTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "MaktabBookTests_" + Guid.NewGuid());
        _folders = new AppFolders(
            Root: _tempDir,
            Data: Path.Combine(_tempDir, "Data"),
            Logs: Path.Combine(_tempDir, "Logs"),
            Backups: Path.Combine(_tempDir, "Backups"),
            Reports: Path.Combine(_tempDir, "Reports"),
            Logos: Path.Combine(_tempDir, "Logos"));

        DirectoryBootstrapper.EnsureFoldersExist(_folders);

        _connectionStringProvider = new ConnectionStringProvider(_folders);
        var initializer = new SqliteDatabaseInitializer(_connectionStringProvider);
        initializer.InitializeAsync().GetAwaiter().GetResult();

        _bookRepository = new SqliteBookRepository(_connectionStringProvider);
        _studentRepository = new SqliteStudentRepository(_connectionStringProvider);
        _classSubjectRepository = new SqliteClassSubjectRepository(_connectionStringProvider);
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        try { Directory.Delete(_tempDir, recursive: true); } catch { }
    }

    [Fact]
    public async Task BookCrud_WorksEndToEnd()
    {
        var bookId = await _bookRepository.CreateBookAsync(new Book
        {
            Title = "Clean Code",
            Author = "Robert Martin",
            ISBN = "978-0132350884",
            Category = "Programming",
            TotalCopies = 5,
            AvailableCopies = 5
        });

        Assert.True(bookId > 0);

        var books = await _bookRepository.GetBooksAsync();
        Assert.Single(books);
        Assert.Equal(5, books[0].TotalCopies);

        await _bookRepository.UpdateBookAsync(new Book
        {
            BookId = bookId,
            Title = "Clean Code 2",
            Author = "Robert Martin",
            ISBN = "978-0132350884",
            Category = "Programming",
            TotalCopies = 6,
            AvailableCopies = 6
        });

        books = await _bookRepository.GetBooksAsync();
        Assert.Equal("Clean Code 2", books[0].Title);
        Assert.Equal(6, books[0].TotalCopies);

        await _bookRepository.DeleteBookAsync(bookId);
        books = await _bookRepository.GetBooksAsync();
        Assert.Empty(books);
    }

    [Fact]
    public async Task IssueAndReturnBook_UpdatesAvailableCopiesAndStatus()
    {
        var classId = await _classSubjectRepository.CreateClassAsync(new SchoolClass { GradeName = "صنف هفتم", NumberOfSubjects = 8 });
        var studentId = await _studentRepository.CreateStudentAsync(new Student
        {
            FirstName = "Ahmad", LastName = "Karimi", FatherName = "Mohammad", ClassId = classId, RollNumber = "101"
        });

        var bookId = await _bookRepository.CreateBookAsync(new Book
        {
            Title = "Clean Code",
            Author = "Robert Martin",
            TotalCopies = 2,
            AvailableCopies = 2
        });

        var issueId = await _bookRepository.IssueBookAsync(new BookIssue
        {
            BookId = bookId,
            StudentId = studentId,
            IssueDate = DateTime.Today,
            DueDate = DateTime.Today.AddDays(7),
            Status = BookIssueStatus.Issued
        });

        var book = await _bookRepository.GetBookByIdAsync(bookId);
        Assert.Equal(1, book!.AvailableCopies);

        await _bookRepository.ReturnBookAsync(issueId, DateTime.Today);

        book = await _bookRepository.GetBookByIdAsync(bookId);
        Assert.Equal(2, book!.AvailableCopies);

        var issues = await _bookRepository.GetIssuesAsync();
        Assert.Single(issues);
        Assert.Equal(BookIssueStatus.Returned, issues[0].Status);
    }

    [Fact]
    public async Task IssueBook_WhenNoAvailableCopies_ThrowsInvalidOperationException()
    {
        var classId = await _classSubjectRepository.CreateClassAsync(new SchoolClass { GradeName = "صنف هفتم", NumberOfSubjects = 8 });
        var student1 = await _studentRepository.CreateStudentAsync(new Student { FirstName = "A", LastName = "B", FatherName = "C", ClassId = classId, RollNumber = "1" });
        var student2 = await _studentRepository.CreateStudentAsync(new Student { FirstName = "D", LastName = "E", FatherName = "F", ClassId = classId, RollNumber = "2" });

        var bookId = await _bookRepository.CreateBookAsync(new Book
        {
            Title = "Only One",
            Author = "Author",
            TotalCopies = 1,
            AvailableCopies = 1
        });

        await _bookRepository.IssueBookAsync(new BookIssue { BookId = bookId, StudentId = student1, IssueDate = DateTime.Today, DueDate = DateTime.Today.AddDays(7), Status = BookIssueStatus.Issued });

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await _bookRepository.IssueBookAsync(new BookIssue { BookId = bookId, StudentId = student2, IssueDate = DateTime.Today, DueDate = DateTime.Today.AddDays(7), Status = BookIssueStatus.Issued });
        });
    }
}
