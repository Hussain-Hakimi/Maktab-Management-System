using ClosedXML.Excel;
using Maktab.Application.Abstractions;
using Maktab.Application.Services;
using Maktab.Domain.Entities;
using Maktab.Infrastructure.Reports;

namespace Maktab.Tests;

public class BulkImportExcelTests
{
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

        public Task UpdateStudentAsync(int studentId, string firstName, string lastName, string fatherName, int classId, string rollNumber, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task RemoveStudentAsync(int studentId, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<int> GetNextRollNumberAsync(int classId, CancellationToken cancellationToken = default)
        {
            var classStudents = Students.Where(s => s.ClassId == classId).ToList();
            if (!classStudents.Any()) return Task.FromResult(1);
            var maxRoll = classStudents.Max(s => int.TryParse(s.RollNumber, out var r) ? r : 0);
            return Task.FromResult(maxRoll + 1);
        }
    }

    private sealed class InMemoryClassSubjectService : IClassSubjectService
    {
        public List<SchoolClass> Classes { get; } = [];
        public List<Subject> Subjects { get; } = [];

        public Task<IReadOnlyList<SchoolClass>> GetClassesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<SchoolClass>>(Classes);

        public Task<IReadOnlyList<Subject>> GetSubjectsByClassAsync(int classId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Subject>>(Subjects.Where(s => s.ClassId == classId).ToList());

        public Task<IReadOnlyList<Subject>> GetAllSubjectsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Subject>>(Subjects);

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

        public Task<IReadOnlyList<StudentExamMarkDto>> GetClassSubjectMarksAsync(int classId, int subjectId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<StudentExamMarkDto>> GetStudentMarksAsync(int studentId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<StudentExamMarkDto>> GetStudentMarksForYearAsync(int studentId, int academicYearId, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task SaveMarksBatchAsync(IEnumerable<SaveExamMarkDto> marks, CancellationToken cancellationToken = default)
        {
            SavedMarks.AddRange(marks);
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryAttendanceService : IAttendanceService
    {
        public List<SaveAttendanceDto> SavedAttendance { get; } = [];

        public Task<IReadOnlyList<StudentAttendanceDto>> GetClassAttendanceForDateAsync(int classId, DateTime date, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task SaveAttendanceBatchAsync(IEnumerable<SaveAttendanceDto> attendance, CancellationToken cancellationToken = default)
        {
            SavedAttendance.AddRange(attendance);
            return Task.CompletedTask;
        }
        public Task<int> GetStudentAbsenceDaysAsync(int studentId, string academicYear, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<StudentAttendanceSummaryDto?> GetStudentAttendanceSummaryAsync(int studentId, int academicYearId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<StudentAttendanceSummaryDto>> GetClassAttendanceSummaryAsync(int classId, int academicYearId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<MonthlyAttendanceRowDto>> GetMonthlyAttendanceReportAsync(int classId, int year, int month, int academicYearId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    [Fact]
    public async Task ImportStudentsFromFile_WithExcelFile_ImportsSuccessfully()
    {
        // Arrange
        var studentService = new InMemoryStudentService();
        var classService = new InMemoryClassSubjectService();
        var markService = new InMemoryExamMarkService();
        var attendanceService = new InMemoryAttendanceService();
        var excelReader = new ExcelReader();

        classService.Classes.Add(new SchoolClass { ClassId = 1, GradeName = "Grade 1", NumberOfSubjects = 2 });

        var bulkImport = new BulkImportService(studentService, classService, markService, attendanceService, excelReader);

        var tempFile = Path.Combine(Path.GetTempPath(), "MaktabBulkImportExcelTest_" + Guid.NewGuid() + ".xlsx");
        using (var workbook = new XLWorkbook())
        {
            var sheet = workbook.Worksheets.Add("Students");
            sheet.Cell(1, 1).Value = "FirstName";
            sheet.Cell(1, 2).Value = "LastName";
            sheet.Cell(1, 3).Value = "FatherName";
            sheet.Cell(1, 4).Value = "RollNumber";
            sheet.Cell(1, 5).Value = "ClassName";
            sheet.Cell(2, 1).Value = "Ahmad";
            sheet.Cell(2, 2).Value = "Karimi";
            sheet.Cell(2, 3).Value = "Mohammad";
            sheet.Cell(2, 4).Value = "101";
            sheet.Cell(2, 5).Value = "Grade 1";
            workbook.SaveAs(tempFile);
        }

        try
        {
            // Act
            var result = await bulkImport.ImportStudentsFromFileAsync(tempFile);

            // Assert
            Assert.Equal(1, result.SuccessCount);
            Assert.Equal(0, result.FailureCount);
            Assert.Single(studentService.Students);
            Assert.Equal("Ahmad", studentService.Students[0].FirstName);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }
}
