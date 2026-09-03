using Microsoft.Data.Sqlite;
using Maktab.Domain.Entities;
using Maktab.Infrastructure.Persistence;

namespace Maktab.Tests;

public sealed class PromotionTransactionIntegrationTests : IDisposable
{
    private readonly string _tempDir;
    private readonly AppFolders _folders;
    private readonly ConnectionStringProvider _connectionStrings;
    private readonly SqliteClassSubjectRepository _classRepository;
    private readonly SqliteStudentRepository _studentRepository;
    private readonly SqliteAcademicYearRepository _academicYearRepository;
    private readonly SqliteStudentAcademicEnrollmentRepository _enrollmentRepository;
    private readonly SqlitePromotionTransactionRepository _transactionRepository;

    public PromotionTransactionIntegrationTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "MaktabPromotionTransactionTests_" + Guid.NewGuid());
        _folders = new AppFolders(
            Root: _tempDir,
            Data: Path.Combine(_tempDir, "Data"),
            Logs: Path.Combine(_tempDir, "Logs"),
            Backups: Path.Combine(_tempDir, "Backups"),
            Reports: Path.Combine(_tempDir, "Reports"),
            Logos: Path.Combine(_tempDir, "Logos"));

        DirectoryBootstrapper.EnsureFoldersExist(_folders);
        _connectionStrings = new ConnectionStringProvider(_folders);
        new SqliteDatabaseInitializer(_connectionStrings).InitializeAsync().GetAwaiter().GetResult();

        _classRepository = new SqliteClassSubjectRepository(_connectionStrings);
        _studentRepository = new SqliteStudentRepository(_connectionStrings);
        _academicYearRepository = new SqliteAcademicYearRepository(_connectionStrings);
        _enrollmentRepository = new SqliteStudentAcademicEnrollmentRepository(_connectionStrings);
        _transactionRepository = new SqlitePromotionTransactionRepository(_connectionStrings);
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        try
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, recursive: true);
        }
        catch
        {
        }
    }

    [Fact]
    public async Task ApplyAsync_CommitsStudentEnrollmentAndHistoryAtomically()
    {
        var fromClassId = await _classRepository.CreateClassAsync(new SchoolClass
        {
            GradeName = "Grade 1",
            NumberOfSubjects = 1
        });
        var toClassId = await _classRepository.CreateClassAsync(new SchoolClass
        {
            GradeName = "Grade 2",
            NumberOfSubjects = 1
        });

        var sourceYear = await _academicYearRepository.GetActiveAsync()
            ?? throw new InvalidOperationException("Test database did not create an active academic year.");
        var nextYearId = await _academicYearRepository.CreateAsync(new AcademicYear
        {
            YearName = sourceYear.YearName + "-NEXT",
            StartDate = sourceYear.EndDate.AddDays(1),
            EndDate = sourceYear.EndDate.AddDays(366),
            IsActive = false
        });

        var studentId = await _studentRepository.CreateStudentAsync(new Student
        {
            FirstName = "Integration",
            LastName = "Student",
            FatherName = "Parent",
            ClassId = fromClassId,
            RollNumber = "INT-001",
            RegistrationDate = DateTime.Now
        });

        await _transactionRepository.ApplyAsync(
            new Student
            {
                StudentId = studentId,
                FirstName = "Integration",
                LastName = "Student",
                FatherName = "Parent",
                ClassId = toClassId,
                RollNumber = "INT-001",
                RegistrationDate = DateTime.Now
            },
            new StudentPromotionHistory
            {
                StudentId = studentId,
                FromClassId = fromClassId,
                ToClassId = toClassId,
                AcademicYearId = sourceYear.AcademicYearId,
                Result = "Promoted",
                PromotionDate = DateTime.Now
            },
            new StudentAcademicEnrollment
            {
                StudentId = studentId,
                AcademicYearId = nextYearId,
                ClassId = toClassId,
                RollNumber = "INT-001",
                EnrollmentDate = sourceYear.EndDate.AddDays(1),
                Status = "Active"
            });

        var student = await _studentRepository.GetStudentByIdAsync(studentId);
        var enrollment = await _enrollmentRepository.GetByStudentAndAcademicYearAsync(studentId, nextYearId);
        var history = await QueryHistoryCountAsync(studentId);

        Assert.NotNull(student);
        Assert.Equal(toClassId, student!.ClassId);
        Assert.NotNull(enrollment);
        Assert.Equal(toClassId, enrollment!.ClassId);
        Assert.Equal(1, history);
    }

    [Fact]
    public async Task ApplyAsync_WhenEnrollmentFails_RollsBackStudentAndHistory()
    {
        var fromClassId = await _classRepository.CreateClassAsync(new SchoolClass
        {
            GradeName = "Grade 3",
            NumberOfSubjects = 1
        });
        var toClassId = await _classRepository.CreateClassAsync(new SchoolClass
        {
            GradeName = "Grade 4",
            NumberOfSubjects = 1
        });

        var sourceYear = await _academicYearRepository.GetActiveAsync()
            ?? throw new InvalidOperationException("Test database did not create an active academic year.");
        var nextYearId = await _academicYearRepository.CreateAsync(new AcademicYear
        {
            YearName = sourceYear.YearName + "-ROLLBACK",
            StartDate = sourceYear.EndDate.AddDays(2),
            EndDate = sourceYear.EndDate.AddDays(367),
            IsActive = false
        });

        var studentId = await _studentRepository.CreateStudentAsync(new Student
        {
            FirstName = "Rollback",
            LastName = "Student",
            FatherName = "Parent",
            ClassId = fromClassId,
            RollNumber = "INT-002",
            RegistrationDate = DateTime.Now
        });

        await Assert.ThrowsAnyAsync<Exception>(() => _transactionRepository.ApplyAsync(
            new Student
            {
                StudentId = studentId,
                FirstName = "Rollback",
                LastName = "Student",
                FatherName = "Parent",
                ClassId = toClassId,
                RollNumber = "INT-002",
                RegistrationDate = DateTime.Now
            },
            new StudentPromotionHistory
            {
                StudentId = studentId,
                FromClassId = fromClassId,
                ToClassId = toClassId,
                AcademicYearId = sourceYear.AcademicYearId,
                Result = "Promoted",
                PromotionDate = DateTime.Now
            },
            new StudentAcademicEnrollment
            {
                StudentId = studentId,
                AcademicYearId = nextYearId,
                ClassId = toClassId,
                RollNumber = "INT-002",
                EnrollmentDate = sourceYear.EndDate.AddDays(2),
                Status = "InvalidStatus"
            }));

        var student = await _studentRepository.GetStudentByIdAsync(studentId);
        var enrollment = await _enrollmentRepository.GetByStudentAndAcademicYearAsync(studentId, nextYearId);
        var history = await QueryHistoryCountAsync(studentId);

        Assert.NotNull(student);
        Assert.Equal(fromClassId, student!.ClassId);
        Assert.Null(enrollment);
        Assert.Equal(0, history);
    }

    private async Task<int> QueryHistoryCountAsync(int studentId)
    {
        await using var connection = new SqliteConnection(_connectionStrings.GetConnectionString());
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(1) FROM tbl_StudentPromotionHistory WHERE StudentID = $studentId;";
        command.Parameters.AddWithValue("$studentId", studentId);
        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }
}
