using Maktab.Application.Abstractions;
using Maktab.Application.Services;
using Maktab.Domain.Entities;
using Maktab.Domain.Enums;

namespace Maktab.Tests;

public class BulkImportMarksAttendanceTests
{
    // ----- In‑memory implementations -----

    private sealed class InMemoryStudentService : IStudentService
    {
        public List<Student> Students { get; } = [];

        public Task<IReadOnlyList<Student>> GetAllStudentsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Student>>(Students);

        public Task<IReadOnlyList<Student>> GetStudentsByClassAsync(int classId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Student>>(Students.Where(s => s.ClassId == classId).ToList());

        public Task<Student?> GetStudentByIdAsync(int studentId, CancellationToken cancellationToken = default)
            => Task.FromResult(Students.FirstOrDefault(s => s.StudentId == studentId));

        public Task<int> RegisterStudentAsync(string firstName, string lastName, string fatherName, int classId, string rollNumber, CancellationToken cancellationToken = default)
        {
            var student = new Student
            {
                StudentId = Students.Count + 1,
                FirstName = firstName,
                LastName = lastName,
                FatherName = fatherName,
                ClassId = classId,
                RollNumber = rollNumber
            };
            Students.Add(student);
            return Task.FromResult(student.StudentId);
        }

        public Task UpdateStudentAsync(int studentId, string firstName, string lastName, string fatherName, int classId, string rollNumber, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task RemoveStudentAsync(int studentId, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();
    }

    private sealed class InMemoryClassSubjectService : IClassSubjectService
    {
        public List<SchoolClass> Classes { get; } = [];
        public List<Subject> Subjects { get; } = [];

        public Task<IReadOnlyList<SchoolClass>> GetClassesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<SchoolClass>>(Classes);

        public Task<IReadOnlyList<Subject>> GetSubjectsByClassAsync(int classId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Subject>>(Subjects.Where(s => s.ClassId == classId).ToList());

        public Task<int> CreateClassAsync(string gradeName, int numberOfSubjects, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task UpdateClassAsync(int classId, string gradeName, int numberOfSubjects, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task DeleteClassAsync(int classId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<int> CreateSubjectAsync(int classId, string subjectName, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task UpdateSubjectAsync(int subjectId, int classId, string subjectName, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task DeleteSubjectAsync(int subjectId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private sealed class InMemoryExamMarkService : IExamMarkService
    {
        public List<SaveExamMarkDto> SavedMarks { get; } = [];

        public Task<IReadOnlyList<StudentExamMarkDto>> GetClassSubjectMarksAsync(int classId, int subjectId, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<IReadOnlyList<StudentExamMarkDto>> GetStudentMarksAsync(int studentId, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task SaveMarksBatchAsync(IEnumerable<SaveExamMarkDto> marks, CancellationToken cancellationToken = default)
        {
            SavedMarks.AddRange(marks);
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryAttendanceService : IAttendanceService
    {
        public List<SaveAttendanceDto> SavedAttendance { get; } = [];

        public Task<IReadOnlyList<StudentAttendanceDto>> GetClassAttendanceForDateAsync(int classId, DateTime date, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task SaveAttendanceBatchAsync(IEnumerable<SaveAttendanceDto> attendance, CancellationToken cancellationToken = default)
        {
            SavedAttendance.AddRange(attendance);
            return Task.CompletedTask;
        }

        public Task<int> GetStudentAbsenceDaysAsync(int studentId, string academicYear, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<StudentAttendanceSummaryDto?> GetStudentAttendanceSummaryAsync(int studentId, int academicYearId, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<IReadOnlyList<StudentAttendanceSummaryDto>> GetClassAttendanceSummaryAsync(int classId, int academicYearId, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<IReadOnlyList<MonthlyAttendanceRowDto>> GetMonthlyAttendanceReportAsync(int classId, int year, int month, int academicYearId, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();
    }

    // ----- Tests -----

    [Fact]
    public async Task ImportMarks_WithValidData_SavesCorrectly()
    {
        // Arrange
        var studentService = new InMemoryStudentService();
        var classService = new InMemoryClassSubjectService();
        var examMarkService = new InMemoryExamMarkService();
        var attendanceService = new InMemoryAttendanceService();

        // Add a class and student
        classService.Classes.Add(new SchoolClass { ClassId = 1, GradeName = "Grade 1", NumberOfSubjects = 2 });
        classService.Subjects.Add(new Subject { SubjectId = 1, ClassId = 1, SubjectName = "Math" });
        studentService.Students.Add(new Student { StudentId = 1, FirstName = "Ahmad", LastName = "Karimi", FatherName = "Mohammad", ClassId = 1, RollNumber = "101" });

        var bulkImport = new BulkImportService(studentService, classService, examMarkService, attendanceService);

        var csv = "RollNumber,MidtermScore,FinalScore\n101,30,50";

        // Act
        var result = await bulkImport.ImportMarksFromCsvAsync(csv, classId: 1, subjectId: 1, academicYearId: 1);

        // Assert
        Assert.Equal(1, result.SuccessCount);
        Assert.Equal(0, result.FailureCount);
        Assert.Single(examMarkService.SavedMarks);
        Assert.Equal(30m, examMarkService.SavedMarks[0].MidtermScore);
        Assert.Equal(50m, examMarkService.SavedMarks[0].FinalScore);
    }

    [Fact]
    public async Task ImportMarks_WithUnknownRollNumber_ReportsError()
    {
        var studentService = new InMemoryStudentService();
        var classService = new InMemoryClassSubjectService();
        var examMarkService = new InMemoryExamMarkService();
        var attendanceService = new InMemoryAttendanceService();

        classService.Classes.Add(new SchoolClass { ClassId = 1, GradeName = "Grade 1", NumberOfSubjects = 2 });
        classService.Subjects.Add(new Subject { SubjectId = 1, ClassId = 1, SubjectName = "Math" });

        var bulkImport = new BulkImportService(studentService, classService, examMarkService, attendanceService);

        var csv = "RollNumber,MidtermScore,FinalScore\n999,30,50";

        var result = await bulkImport.ImportMarksFromCsvAsync(csv, classId: 1, subjectId: 1, academicYearId: 1);

        Assert.Equal(0, result.SuccessCount);
        Assert.Equal(1, result.FailureCount);
        Assert.Single(result.Errors);
        Assert.Empty(examMarkService.SavedMarks);
    }

    [Fact]
    public async Task ImportAttendance_WithValidStatuses_SavesCorrectly()
    {
        // Arrange
        var studentService = new InMemoryStudentService();
        var classService = new InMemoryClassSubjectService();
        var examMarkService = new InMemoryExamMarkService();
        var attendanceService = new InMemoryAttendanceService();

        classService.Classes.Add(new SchoolClass { ClassId = 1, GradeName = "Grade 1", NumberOfSubjects = 2 });
        studentService.Students.Add(new Student { StudentId = 1, FirstName = "A", LastName = "B", FatherName = "C", ClassId = 1, RollNumber = "101" });

        var bulkImport = new BulkImportService(studentService, classService, examMarkService, attendanceService);

        var csv = "RollNumber,Date,Status\n101,2024-01-15,Present";

        var result = await bulkImport.ImportAttendanceFromCsvAsync(csv, classId: 1, academicYearId: 1);

        Assert.Equal(1, result.SuccessCount);
        Assert.Equal(0, result.FailureCount);
        Assert.Single(attendanceService.SavedAttendance);
        Assert.Equal(AttendanceStatus.Present, attendanceService.SavedAttendance[0].Status);
    }

    [Fact]
    public async Task ImportAttendance_WithInvalidStatus_ReportsError()
    {
        var studentService = new InMemoryStudentService();
        var classService = new InMemoryClassSubjectService();
        var examMarkService = new InMemoryExamMarkService();
        var attendanceService = new InMemoryAttendanceService();

        classService.Classes.Add(new SchoolClass { ClassId = 1, GradeName = "Grade 1", NumberOfSubjects = 2 });
        studentService.Students.Add(new Student { StudentId = 1, FirstName = "A", LastName = "B", FatherName = "C", ClassId = 1, RollNumber = "101" });

        var bulkImport = new BulkImportService(studentService, classService, examMarkService, attendanceService);

        var csv = "RollNumber,Date,Status\n101,2024-01-15,Unknown";

        var result = await bulkImport.ImportAttendanceFromCsvAsync(csv, classId: 1, academicYearId: 1);

        Assert.Equal(0, result.SuccessCount);
        Assert.Equal(1, result.FailureCount);
        Assert.Empty(attendanceService.SavedAttendance);
    }
}
