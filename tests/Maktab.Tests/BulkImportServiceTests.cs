using Maktab.Application.Abstractions;
using Maktab.Application.Services;
using Maktab.Domain.Entities;

namespace Maktab.Tests;

public class BulkImportServiceTests
{
    private sealed class MockStudentService : IStudentService
    {
        public int RegisterCount { get; private set; }
        public Task<IReadOnlyList<Student>> GetAllStudentsAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<Student>> GetStudentsByClassAsync(int classId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<Student?> GetStudentByIdAsync(int studentId, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<int> RegisterStudentAsync(string firstName, string lastName, string fatherName, int classId, string rollNumber, CancellationToken cancellationToken = default)
        {
            RegisterCount++;
            return Task.FromResult(RegisterCount);
        }

        public Task UpdateStudentAsync(int studentId, string firstName, string lastName, string fatherName, int classId, string rollNumber, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task RemoveStudentAsync(int studentId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private sealed class MockClassSubjectService : IClassSubjectService
    {
        public Task<IReadOnlyList<SchoolClass>> GetClassesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<SchoolClass>>([new SchoolClass { ClassId = 1, GradeName = "صنف هفتم", NumberOfSubjects = 8 }]);

        public Task<IReadOnlyList<Subject>> GetSubjectsByClassAsync(int classId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<int> CreateClassAsync(string gradeName, int numberOfSubjects, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task UpdateClassAsync(int classId, string gradeName, int numberOfSubjects, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task DeleteClassAsync(int classId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<int> CreateSubjectAsync(int classId, string subjectName, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task UpdateSubjectAsync(int subjectId, int classId, string subjectName, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task DeleteSubjectAsync(int subjectId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private sealed class MockExamMarkService : IExamMarkService
    {
        public Task<IReadOnlyList<StudentExamMarkDto>> GetClassSubjectMarksAsync(int classId, int subjectId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<StudentExamMarkDto>> GetStudentMarksAsync(int studentId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<StudentExamMarkDto>> GetStudentMarksForYearAsync(int studentId, int academicYearId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task SaveMarksBatchAsync(IEnumerable<SaveExamMarkDto> marks, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private sealed class MockAttendanceService : IAttendanceService
    {
        public Task<IReadOnlyList<StudentAttendanceDto>> GetClassAttendanceForDateAsync(int classId, DateTime date, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task SaveAttendanceBatchAsync(IEnumerable<SaveAttendanceDto> attendance, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<int> GetStudentAbsenceDaysAsync(int studentId, string academicYear, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<StudentAttendanceSummaryDto?> GetStudentAttendanceSummaryAsync(int studentId, int academicYearId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<StudentAttendanceSummaryDto>> GetClassAttendanceSummaryAsync(int classId, int academicYearId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<MonthlyAttendanceRowDto>> GetMonthlyAttendanceReportAsync(int classId, int year, int month, int academicYearId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private sealed class MockExcelReader : IExcelReader
    {
        public IReadOnlyList<string[]> ReadRows(string filePath) => throw new NotImplementedException();
    }


    [Fact]
    public async Task ImportStudents_WithValidCsv_ImportsAllRows()
    {
        var studentService = new MockStudentService();
        var classService = new MockClassSubjectService();
        var examService = new MockExamMarkService();
        var attendanceService = new MockAttendanceService();
        var excelReader = new MockExcelReader();
        var bulkImport = new BulkImportService(studentService, classService, examService, attendanceService, excelReader);

        var csv = "FirstName,LastName,FatherName,RollNumber,ClassName\nAli,Ahmadi,Mohammad,101,صنف هفتم\nZahra,Hussaini,Ali,102,صنف هفتم";

        var result = await bulkImport.ImportStudentsFromCsvAsync(csv);

        Assert.Equal(2, result.TotalRows);
        Assert.Equal(2, result.SuccessCount);
        Assert.Equal(0, result.FailureCount);
        Assert.Equal(2, studentService.RegisterCount);
    }

    [Fact]
    public async Task ImportStudents_WithInvalidClassName_ReportsError()
    {
        var studentService = new MockStudentService();
        var classService = new MockClassSubjectService();
        var examService = new MockExamMarkService();
        var attendanceService = new MockAttendanceService();
        var excelReader = new MockExcelReader();
        var bulkImport = new BulkImportService(studentService, classService, examService, attendanceService, excelReader);

        var csv = "FirstName,LastName,FatherName,RollNumber,ClassName\nAli,Ahmadi,Mohammad,101,صنف نهم";

        var result = await bulkImport.ImportStudentsFromCsvAsync(csv);

        Assert.Equal(1, result.TotalRows);
        Assert.Equal(0, result.SuccessCount);
        Assert.Equal(1, result.FailureCount);
        Assert.Single(result.Errors);
    }
}
