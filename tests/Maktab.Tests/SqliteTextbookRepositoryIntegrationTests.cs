using Microsoft.Data.Sqlite;
using Maktab.Application.Abstractions;
using Maktab.Domain.Entities;
using Maktab.Domain.Enums;
using Maktab.Infrastructure.Persistence;

namespace Maktab.Tests;

public class SqliteTextbookRepositoryIntegrationTests : IDisposable
{
    private readonly string _tempDir;
    private readonly AppFolders _folders;
    private readonly ConnectionStringProvider _connectionStringProvider;
    private readonly SqliteTextbookRepository _textbookRepository;
    private readonly SqliteStudentRepository _studentRepository;
    private readonly SqliteClassSubjectRepository _classSubjectRepository;

    public SqliteTextbookRepositoryIntegrationTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "MaktabTextbookTests_" + Guid.NewGuid());
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

        _textbookRepository = new SqliteTextbookRepository(_connectionStringProvider);
        _studentRepository = new SqliteStudentRepository(_connectionStringProvider);
        _classSubjectRepository = new SqliteClassSubjectRepository(_connectionStringProvider);
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        try { Directory.Delete(_tempDir, recursive: true); } catch { }
    }

    [Fact]
    public async Task TextbookCrud_WorksEndToEnd()
    {
        var classId = await _classSubjectRepository.CreateClassAsync(new SchoolClass { GradeName = "صنف هفتم", NumberOfSubjects = 8 });
        var textbookId = await _textbookRepository.CreateTextbookAsync(new Textbook
        {
            Title = "Math",
            Subject = "Mathematics",
            ClassId = classId,
            TotalCopies = 10,
            AvailableCopies = 10
        });

        Assert.True(textbookId > 0);

        var textbooks = await _textbookRepository.GetTextbooksAsync();
        Assert.Single(textbooks);
        Assert.Equal(10, textbooks[0].TotalCopies);

        await _textbookRepository.UpdateTextbookAsync(new Textbook
        {
            TextbookId = textbookId,
            Title = "Advanced Math",
            Subject = "Mathematics",
            ClassId = classId,
            TotalCopies = 12,
            AvailableCopies = 12
        });

        textbooks = await _textbookRepository.GetTextbooksAsync();
        Assert.Equal("Advanced Math", textbooks[0].Title);
        Assert.Equal(12, textbooks[0].TotalCopies);

        await _textbookRepository.DeleteTextbookAsync(textbookId);
        textbooks = await _textbookRepository.GetTextbooksAsync();
        Assert.Empty(textbooks);
    }

    [Fact]
    public async Task IssueAndReturnTextbook_UpdatesAvailableCopiesAndStatus()
    {
        var classId = await _classSubjectRepository.CreateClassAsync(new SchoolClass { GradeName = "صنف هفتم", NumberOfSubjects = 8 });
        var studentId = await _studentRepository.CreateStudentAsync(new Student
        {
            FirstName = "Ahmad", LastName = "Karimi", FatherName = "Mohammad", ClassId = classId, RollNumber = "101"
        });

        var textbookId = await _textbookRepository.CreateTextbookAsync(new Textbook
        {
            Title = "Math",
            Subject = "Mathematics",
            ClassId = classId,
            TotalCopies = 5,
            AvailableCopies = 5
        });

        var issueId = await _textbookRepository.IssueTextbookAsync(new TextbookIssue
        {
            TextbookId = textbookId,
            StudentId = studentId,
            IssueDate = DateTime.Today,
            Status = TextbookIssueStatus.Issued
        });

        var textbook = await _textbookRepository.GetTextbookByIdAsync(textbookId);
        Assert.Equal(4, textbook!.AvailableCopies);

        await _textbookRepository.ReturnTextbookAsync(issueId, DateTime.Today);

        textbook = await _textbookRepository.GetTextbookByIdAsync(textbookId);
        Assert.Equal(5, textbook!.AvailableCopies);

        var issues = await _textbookRepository.GetIssuesAsync();
        Assert.Single(issues);
        Assert.Equal(TextbookIssueStatus.Returned, issues[0].Status);
    }

    [Fact]
    public async Task IssueTextbook_WhenNoAvailableCopies_ThrowsInvalidOperationException()
    {
        var classId = await _classSubjectRepository.CreateClassAsync(new SchoolClass { GradeName = "صنف هفتم", NumberOfSubjects = 8 });
        var student1 = await _studentRepository.CreateStudentAsync(new Student { FirstName = "A", LastName = "B", FatherName = "C", ClassId = classId, RollNumber = "1" });
        var student2 = await _studentRepository.CreateStudentAsync(new Student { FirstName = "D", LastName = "E", FatherName = "F", ClassId = classId, RollNumber = "2" });

        var textbookId = await _textbookRepository.CreateTextbookAsync(new Textbook
        {
            Title = "Only One",
            Subject = "Test",
            ClassId = classId,
            TotalCopies = 1,
            AvailableCopies = 1
        });

        await _textbookRepository.IssueTextbookAsync(new TextbookIssue { TextbookId = textbookId, StudentId = student1, IssueDate = DateTime.Today, Status = TextbookIssueStatus.Issued });

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await _textbookRepository.IssueTextbookAsync(new TextbookIssue { TextbookId = textbookId, StudentId = student2, IssueDate = DateTime.Today, Status = TextbookIssueStatus.Issued });
        });
    }
}
