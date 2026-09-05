using Maktab.Application.Abstractions;
using Maktab.Application.Services;
using Maktab.Domain.Entities;

namespace Maktab.Tests;

public sealed class BulkImportPreviewServiceTests
{
    private sealed class StudentService : IStudentService
    {
        public List<Student> Students { get; } = [];
        public Task<IReadOnlyList<Student>> GetAllStudentsAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Student>>(Students);
        public Task<IReadOnlyList<Student>> GetStudentsByClassAsync(int classId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Student>>(Students.Where(s => s.ClassId == classId).ToList());
        public Task<Student?> GetStudentByIdAsync(int studentId, CancellationToken cancellationToken = default) => Task.FromResult(Students.FirstOrDefault(s => s.StudentId == studentId));
        public Task<int> RegisterStudentAsync(string firstName, string lastName, string fatherName, int classId, string rollNumber, CancellationToken cancellationToken = default) => Task.FromResult(0);
        public Task UpdateStudentAsync(int studentId, string firstName, string lastName, string fatherName, int classId, string rollNumber, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RemoveStudentAsync(int studentId, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<int> GetNextRollNumberAsync(int classId, CancellationToken cancellationToken = default) => Task.FromResult(1);
    }
    private sealed class ClassSubjectService : IClassSubjectService
    {
        public List<SchoolClass> Classes { get; } = [];
        public List<Subject> Subjects { get; } = [];
        public Task<IReadOnlyList<SchoolClass>> GetClassesAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<SchoolClass>>(Classes);
        public Task<IReadOnlyList<Subject>> GetSubjectsByClassAsync(int classId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Subject>>(Subjects.Where(s => s.ClassId == classId).ToList());
        public Task<IReadOnlyList<Subject>> GetAllSubjectsAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Subject>>(Subjects);
        public Task<int> CreateClassAsync(string gradeName, int numberOfSubjects, CancellationToken cancellationToken = default) => Task.FromResult(0);
        public Task UpdateClassAsync(int classId, string gradeName, int numberOfSubjects, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DeleteClassAsync(int classId, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<int> CreateSubjectAsync(int classId, string subjectName, CancellationToken cancellationToken = default) => Task.FromResult(0);
        public Task UpdateSubjectAsync(int subjectId, int classId, string subjectName, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DeleteSubjectAsync(int subjectId, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
    private sealed class ExcelReader : IExcelReader
    {
        public IReadOnlyList<string[]> Rows { get; init; } = [];
        public IReadOnlyList<string[]> ReadRows(string filePath) => Rows;
    }
    [Fact]
    public async Task PreviewStudents_WithInvalidRow_ReturnsErrorsWithoutWriting()
    {
        var students = new StudentService(); var classes = new ClassSubjectService();
        classes.Classes.Add(new SchoolClass { ClassId = 1, GradeName = "Grade 1" });
        var service = new BulkImportPreviewService(students, classes, new ExcelReader());
        var csv = "FirstName,LastName,FatherName,RollNumber,ClassName\nAli,Ahmadi,Mohammad,101,Grade 1\nBad,,,,";
        var result = await service.PreviewStudentsFromCsvAsync(csv);
        Assert.Equal(2, result.TotalRows); Assert.Equal(1, result.ValidRows); Assert.Equal(1, result.InvalidRows); Assert.False(result.CanImport); Assert.NotEmpty(result.Errors); Assert.Empty(students.Students);
    }
    [Fact]
    public async Task PreviewMarks_WithValidRows_AllowsImportWithoutSaving()
    {
        var students = new StudentService();
        students.Students.Add(new Student { StudentId = 1, FirstName = "Ali", LastName = "Ahmadi", FatherName = "Mohammad", ClassId = 1, RollNumber = "101" });
        var classes = new ClassSubjectService(); classes.Subjects.Add(new Subject { SubjectId = 10, ClassId = 1, SubjectName = "Math" });
        var service = new BulkImportPreviewService(students, classes, new ExcelReader());
        var result = await service.PreviewMarksFromCsvAsync("RollNumber,MidtermScore,FinalScore\n101,18,50", 1, 10, 1);
        Assert.Equal(1, result.TotalRows); Assert.Equal(1, result.ValidRows); Assert.True(result.CanImport);
    }
    [Fact]
    public async Task PreviewAttendance_WithInvalidStatus_RejectsRow()
    {
        var students = new StudentService();
        students.Students.Add(new Student { StudentId = 1, FirstName = "Ali", LastName = "Ahmadi", FatherName = "Mohammad", ClassId = 1, RollNumber = "101" });
        var service = new BulkImportPreviewService(students, new ClassSubjectService(), new ExcelReader());
        var result = await service.PreviewAttendanceFromCsvAsync("RollNumber,Date,Status\n101,2024-01-15,Unknown", 1, 1);
        Assert.Equal(1, result.InvalidRows); Assert.False(result.CanImport);
    }
}
