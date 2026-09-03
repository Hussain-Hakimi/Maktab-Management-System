using Maktab.Application.Abstractions;
using Maktab.Application.Services;
using Maktab.Domain.Entities;
using Maktab.Domain.Enums;

namespace Maktab.Tests;

public class ExamMarkServiceStudentYearTests
{
    private sealed class MockExamMarkRepository : IExamMarkRepository
    {
        public List<ExamMark> Marks { get; set; } = [];
        public Task<IReadOnlyList<ExamMark>> GetMarksByClassAndSubjectAsync(int classId, int subjectId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<ExamMark>> GetMarksByClassSubjectAndYearAsync(int classId, int subjectId, int academicYearId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<ExamMark>> GetMarksByStudentAsync(int studentId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<ExamMark>> GetMarksByStudentAndYearAsync(int studentId, int academicYearId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<ExamMark>>(Marks.Where(m => m.StudentId == studentId && m.AcademicYearId == academicYearId).ToList());
        public Task<IReadOnlyList<ExamMark>> GetMarksByClassAsync(int classId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task SaveOrUpdateMarkAsync(ExamMark mark, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task SaveOrUpdateMarksBatchAsync(IEnumerable<ExamMark> marks, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private sealed class MockStudentRepository : IStudentRepository
    {
        public Student? Student { get; set; }
        public Task<IReadOnlyList<Student>> GetStudentsAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<Student>> GetStudentsByClassAsync(int classId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<Student?> GetStudentByIdAsync(int studentId, CancellationToken cancellationToken = default) => Task.FromResult(Student);
        public Task<int> CreateStudentAsync(Student student, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task UpdateStudentAsync(Student student, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task DeleteStudentAsync(int studentId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> ExistsByRollNumberAsync(int classId, string rollNumber, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private sealed class MockClassSubjectRepository : IClassSubjectRepository
    {
        public List<Subject> Subjects { get; set; } = [];
        public Task<IReadOnlyList<SchoolClass>> GetClassesAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<Subject>> GetSubjectsByClassAsync(int classId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Subject>>(Subjects);
        public Task<int> CreateClassAsync(SchoolClass schoolClass, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task UpdateClassAsync(SchoolClass schoolClass, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task DeleteClassAsync(int classId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<int> CreateSubjectAsync(Subject subject, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task UpdateSubjectAsync(Subject subject, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task DeleteSubjectAsync(int subjectId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<Subject>> GetAllSubjectsAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Subject>>(Subjects);
    }

    private readonly MockExamMarkRepository _markRepo = new();
    private readonly MockStudentRepository _studentRepo = new();
    private readonly MockClassSubjectRepository _classRepo = new();
    private readonly ExamMarkService _service;

    public ExamMarkServiceStudentYearTests()
    {
        var currentUser = new CurrentUserService
        {
            CurrentUser = new UserDto { UserId = 1, Username = "admin", FullName = "Admin", Role = UserRole.Admin, IsActive = true }
        };
        _service = new ExamMarkService(_markRepo, _studentRepo, _classRepo, new AuthorizationService(currentUser));
    }

    [Fact]
    public async Task GetStudentMarksForYear_ReturnsAllSubjectsWithScores()
    {
        var student = new Student { StudentId = 1, ClassId = 1, FirstName = "A", LastName = "B", FatherName = "C", RollNumber = "1" };
        _studentRepo.Student = student;
        _classRepo.Subjects = [
            new() { SubjectId = 1, ClassId = 1, SubjectName = "ریاضی" },
            new() { SubjectId = 2, ClassId = 1, SubjectName = "فزیک" }
        ];
        _markRepo.Marks = [
            new() { StudentId = 1, SubjectId = 1, MidtermScore = 35m, FinalScore = 50m, AcademicYearId = 1 },
            new() { StudentId = 1, SubjectId = 2, MidtermScore = 30m, FinalScore = 40m, AcademicYearId = 1 }
        ];

        var result = await _service.GetStudentMarksForYearAsync(1, 1);

        Assert.Equal(2, result.Count);
        Assert.Equal("ریاضی", result[0].SubjectName);
        Assert.Equal(85m, result[0].TotalScore);
        Assert.Equal("فزیک", result[1].SubjectName);
        Assert.Equal(70m, result[1].TotalScore);
    }

    [Fact]
    public async Task GetStudentMarksForYear_WhenStudentNotFound_ReturnsEmptyList()
    {
        _studentRepo.Student = null;
        var result = await _service.GetStudentMarksForYearAsync(999, 1);
        Assert.Empty(result);
    }
}
