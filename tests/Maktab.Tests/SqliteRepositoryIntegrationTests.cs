using Microsoft.Data.Sqlite;
using Maktab.Domain.Entities;
using Maktab.Infrastructure.Persistence;

namespace Maktab.Tests;

/// <summary>
/// Integration tests that run the real SQLite repositories against a real
/// (temporary) database file, verifying schema constraints end-to-end:
/// UNIQUE roll numbers, CHECK mark ranges, FK CASCADE and FK RESTRICT.
/// </summary>
public class SqliteRepositoryIntegrationTests : IDisposable
{
    private readonly string _tempDir;
    private readonly AppFolders _folders;
    private readonly ConnectionStringProvider _connectionStringProvider;
    private readonly SqliteClassSubjectRepository _classSubjectRepository;
    private readonly SqliteStudentRepository _studentRepository;
    private readonly SqliteExamMarkRepository _examMarkRepository;

    public SqliteRepositoryIntegrationTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "MaktabDbTests_" + Guid.NewGuid());
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

        _classSubjectRepository = new SqliteClassSubjectRepository(_connectionStringProvider);
        _studentRepository = new SqliteStudentRepository(_connectionStringProvider);
        _examMarkRepository = new SqliteExamMarkRepository(_connectionStringProvider);
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        try
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, recursive: true);
            }
        }
        catch
        {
            // Ignore cleanup failure in temp
        }
    }

    [Fact]
    public async Task ClassCrud_WorksEndToEnd()
    {
        var classId = await _classSubjectRepository.CreateClassAsync(new SchoolClass
        {
            GradeName = "صنف هفتم",
            NumberOfSubjects = 8
        });

        Assert.True(classId > 0);

        var classes = await _classSubjectRepository.GetClassesAsync();
        Assert.Single(classes);
        Assert.Equal("صنف هفتم", classes[0].GradeName);

        await _classSubjectRepository.UpdateClassAsync(new SchoolClass
        {
            ClassId = classId,
            GradeName = "صنف هشتم",
            NumberOfSubjects = 9
        });

        classes = await _classSubjectRepository.GetClassesAsync();
        Assert.Equal("صنف هشتم", classes[0].GradeName);
        Assert.Equal(9, classes[0].NumberOfSubjects);

        await _classSubjectRepository.DeleteClassAsync(classId);
        classes = await _classSubjectRepository.GetClassesAsync();
        Assert.Empty(classes);
    }

    [Fact]
    public async Task CreateStudent_WithDuplicateRollNumberInSameClass_ThrowsSqliteException()
    {
        var classId = await CreateClassAsync();

        await _studentRepository.CreateStudentAsync(new Student
        {
            FirstName = "Ahmad",
            LastName = "Karimi",
            FatherName = "Mohammad",
            ClassId = classId,
            RollNumber = "101"
        });

        // UNIQUE (ClassID, RollNumber) must reject the duplicate at the database level
        await Assert.ThrowsAsync<SqliteException>(async () =>
        {
            await _studentRepository.CreateStudentAsync(new Student
            {
                FirstName = "Mahmood",
                LastName = "Rahimi",
                FatherName = "Ali",
                ClassId = classId,
                RollNumber = "101"
            });
        });
    }

    [Fact]
    public async Task SaveOrUpdateMark_WhenCalledTwice_UpsertsSingleRow()
    {
        var classId = await CreateClassAsync();
        var subjectId = await CreateSubjectAsync(classId, "ریاضی");
        var studentId = await CreateStudentAsync(classId, "101");

        await _examMarkRepository.SaveOrUpdateMarkAsync(new ExamMark
        {
            StudentId = studentId,
            SubjectId = subjectId,
            MidtermScore = 30m,
            FinalScore = 40m
        });

        await _examMarkRepository.SaveOrUpdateMarkAsync(new ExamMark
        {
            StudentId = studentId,
            SubjectId = subjectId,
            MidtermScore = 35m,
            FinalScore = 50m
        });

        var marks = await _examMarkRepository.GetMarksByStudentAsync(studentId);
        Assert.Single(marks);
        Assert.Equal(35m, marks[0].MidtermScore);
        Assert.Equal(50m, marks[0].FinalScore);
    }

    [Fact]
    public async Task SaveOrUpdateMark_WhenScoreViolatesCheckConstraint_ThrowsSqliteException()
    {
        var classId = await CreateClassAsync();
        var subjectId = await CreateSubjectAsync(classId, "ریاضی");
        var studentId = await CreateStudentAsync(classId, "101");

        // MidtermScore must be <= 40 per CHECK constraint
        await Assert.ThrowsAsync<SqliteException>(async () =>
        {
            await _examMarkRepository.SaveOrUpdateMarkAsync(new ExamMark
            {
                StudentId = studentId,
                SubjectId = subjectId,
                MidtermScore = 45m,
                FinalScore = 50m
            });
        });
    }

    [Fact]
    public async Task DeleteClass_WhenStudentsExist_ThrowsSqliteException()
    {
        var classId = await CreateClassAsync();
        await CreateStudentAsync(classId, "101");

        // tbl_Students references tbl_Classes with ON DELETE RESTRICT.
        // This only throws if FK enforcement is active on every connection
        // (ConnectionStringProvider sets ForeignKeys = true).
        await Assert.ThrowsAsync<SqliteException>(async () =>
        {
            await _classSubjectRepository.DeleteClassAsync(classId);
        });
    }

    [Fact]
    public async Task DeleteClass_WhenNoStudents_CascadesSubjects()
    {
        var classId = await CreateClassAsync();
        var subjectId = await CreateSubjectAsync(classId, "ریاضی");

        await _classSubjectRepository.DeleteClassAsync(classId);

        // tbl_Subjects references tbl_Classes with ON DELETE CASCADE
        var subjects = await _classSubjectRepository.GetSubjectsByClassAsync(classId);
        Assert.Empty(subjects);
    }

    [Fact]
    public async Task DeleteStudent_CascadesExamMarks()
    {
        var classId = await CreateClassAsync();
        var subjectId = await CreateSubjectAsync(classId, "ریاضی");
        var studentId = await CreateStudentAsync(classId, "101");

        await _examMarkRepository.SaveOrUpdateMarkAsync(new ExamMark
        {
            StudentId = studentId,
            SubjectId = subjectId,
            MidtermScore = 30m,
            FinalScore = 40m
        });

        await _studentRepository.DeleteStudentAsync(studentId);

        // tbl_ExamMarks references tbl_Students with ON DELETE CASCADE
        var marks = await _examMarkRepository.GetMarksByStudentAsync(studentId);
        Assert.Empty(marks);
    }

    [Fact]
    public async Task GetMarksByClassAndSubject_ReturnsOnlyMatchingRows()
    {
        var classId = await CreateClassAsync();
        var subject1 = await CreateSubjectAsync(classId, "ریاضی");
        var subject2 = await CreateSubjectAsync(classId, "فزیک");
        var studentId = await CreateStudentAsync(classId, "101");

        await _examMarkRepository.SaveOrUpdateMarksBatchAsync(
        [
            new ExamMark { StudentId = studentId, SubjectId = subject1, MidtermScore = 30m, FinalScore = 40m },
            new ExamMark { StudentId = studentId, SubjectId = subject2, MidtermScore = 20m, FinalScore = 30m }
        ]);

        var marks = await _examMarkRepository.GetMarksByClassAndSubjectAsync(classId, subject1);
        Assert.Single(marks);
        Assert.Equal(subject1, marks[0].SubjectId);
    }

    private async Task<int> CreateClassAsync()
    {
        return await _classSubjectRepository.CreateClassAsync(new SchoolClass
        {
            GradeName = "صنف هفتم",
            NumberOfSubjects = 8
        });
    }

    private async Task<int> CreateSubjectAsync(int classId, string subjectName)
    {
        return await _classSubjectRepository.CreateSubjectAsync(new Subject
        {
            ClassId = classId,
            SubjectName = subjectName
        });
    }

    private async Task<int> CreateStudentAsync(int classId, string rollNumber)
    {
        return await _studentRepository.CreateStudentAsync(new Student
        {
            FirstName = "Ahmad",
            LastName = "Karimi",
            FatherName = "Mohammad",
            ClassId = classId,
            RollNumber = rollNumber
        });
    }
}
