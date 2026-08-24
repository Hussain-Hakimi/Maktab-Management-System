using Microsoft.Data.Sqlite;
using Maktab.Application.Abstractions;
using Maktab.Domain.Entities;
using Maktab.Infrastructure.Persistence;

namespace Maktab.Tests;

public class SqlitePromotionHistoryRepositoryIntegrationTests : IDisposable
{
    private readonly string _tempDir;
    private readonly AppFolders _folders;
    private readonly ConnectionStringProvider _connectionStringProvider;
    private readonly SqliteStudentPromotionHistoryRepository _historyRepository;
    private readonly SqliteStudentRepository _studentRepository;
    private readonly SqliteClassSubjectRepository _classSubjectRepository;
    private readonly SqliteAcademicYearRepository _academicYearRepository;

    public SqlitePromotionHistoryRepositoryIntegrationTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "MaktabPromotionHistoryTests_" + Guid.NewGuid());
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

        _historyRepository = new SqliteStudentPromotionHistoryRepository(_connectionStringProvider);
        _studentRepository = new SqliteStudentRepository(_connectionStringProvider);
        _classSubjectRepository = new SqliteClassSubjectRepository(_connectionStringProvider);
        _academicYearRepository = new SqliteAcademicYearRepository(_connectionStringProvider);
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        try { Directory.Delete(_tempDir, recursive: true); } catch { }
    }

    [Fact]
    public async Task AddAndGetHistory_ReturnsRecordWithJoinedData()
    {
        // Arrange
        var class1 = await _classSubjectRepository.CreateClassAsync(new SchoolClass { GradeName = "Grade 1", NumberOfSubjects = 2 });
        var class2 = await _classSubjectRepository.CreateClassAsync(new SchoolClass { GradeName = "Grade 2", NumberOfSubjects = 2 });
        var student = await _studentRepository.CreateStudentAsync(new Student
        {
            FirstName = "Ahmad",
            LastName = "Karimi",
            FatherName = "Mohammad",
            ClassId = class1,
            RollNumber = "101",
            RegistrationDate = DateTime.Now
        });

        var yearId = await _academicYearRepository.CreateAsync(new AcademicYear
        {
            YearName = "۱۴۰۴ - ۱۴۰۵",
            StartDate = new DateTime(2025, 3, 21),
            EndDate = new DateTime(2026, 3, 20),
            IsActive = true
        });

        await _historyRepository.AddAsync(new StudentPromotionHistory
        {
            StudentId = student,
            FromClassId = class1,
            ToClassId = class2,
            AcademicYearId = yearId,
            Result = "Promoted",
            PromotionDate = DateTime.Now
        });

        // Act
        var history = await _historyRepository.GetHistoryAsync(academicYearId: yearId, studentId: null);

        // Assert
        Assert.Single(history);
        Assert.Equal("Ahmad Karimi", history[0].StudentName);
        Assert.Equal("Grade 1", history[0].FromClassName);
        Assert.Equal("Grade 2", history[0].ToClassName);
        Assert.Equal("۱۴۰۴ - ۱۴۰۵", history[0].AcademicYearName);
        Assert.Equal("Promoted", history[0].Result);
    }

    [Fact]
    public async Task GetHistory_WithNoFilters_ReturnsAllRecords()
    {
        // Arrange
        var class1 = await _classSubjectRepository.CreateClassAsync(new SchoolClass { GradeName = "Grade 1", NumberOfSubjects = 2 });
        var student1 = await _studentRepository.CreateStudentAsync(new Student { FirstName = "A", LastName = "B", FatherName = "C", ClassId = class1, RollNumber = "1" });
        var student2 = await _studentRepository.CreateStudentAsync(new Student { FirstName = "D", LastName = "E", FatherName = "F", ClassId = class1, RollNumber = "2" });
        var yearId = await _academicYearRepository.CreateAsync(new AcademicYear { YearName = "۱۴۰۴ - ۱۴۰۵", StartDate = DateTime.Now, EndDate = DateTime.Now.AddYears(1), IsActive = true });

        await _historyRepository.AddAsync(new StudentPromotionHistory { StudentId = student1, FromClassId = class1, ToClassId = null, AcademicYearId = yearId, Result = "Repeat", PromotionDate = DateTime.Now });
        await _historyRepository.AddAsync(new StudentPromotionHistory { StudentId = student2, FromClassId = class1, ToClassId = null, AcademicYearId = yearId, Result = "Conditional", PromotionDate = DateTime.Now });

        var history = await _historyRepository.GetHistoryAsync(null, null);

        Assert.Equal(2, history.Count);
    }
}
