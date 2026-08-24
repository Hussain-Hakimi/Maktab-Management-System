using Maktab.Application.Abstractions;
using Maktab.Application.Services;
using Maktab.Domain.Entities;
using Maktab.Domain.Enums;

namespace Maktab.Tests;

public class ReportCardServiceTests
{
    private sealed class MockPdfGenerator : IPdfReportCardGenerator
    {
        public List<string> GeneratedPaths { get; } = [];
        public List<ReportCardTemplateType> TemplateTypes { get; } = [];

        public Task GeneratePdfReportAsync(
            StudentReportCardDto reportCard,
            string outputFilePath,
            ReportCardTemplateType templateType,
            CancellationToken cancellationToken = default)
        {
            GeneratedPaths.Add(outputFilePath);
            TemplateTypes.Add(templateType);
            return Task.CompletedTask;
        }
    }

    private sealed class MockStudentRepository(Student student) : IStudentRepository
    {
        public Task<IReadOnlyList<Student>> GetStudentsAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Student>>([student]);
        public Task<IReadOnlyList<Student>> GetStudentsByClassAsync(int classId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Student>>([student]);
        public Task<Student?> GetStudentByIdAsync(int studentId, CancellationToken cancellationToken = default) => Task.FromResult<Student?>(student);
        public Task<int> CreateStudentAsync(Student student, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task UpdateStudentAsync(Student student, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task DeleteStudentAsync(int studentId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> ExistsByRollNumberAsync(int classId, string rollNumber, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private sealed class MockClassSubjectRepository(SchoolClass schoolClass, List<Subject> subjects) : IClassSubjectRepository
    {
        public Task<IReadOnlyList<SchoolClass>> GetClassesAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<SchoolClass>>([schoolClass]);
        public Task<IReadOnlyList<Subject>> GetSubjectsByClassAsync(int classId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Subject>>(subjects);
        public Task<int> CreateClassAsync(SchoolClass schoolClass, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task UpdateClassAsync(SchoolClass schoolClass, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task DeleteClassAsync(int classId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<int> CreateSubjectAsync(Subject subject, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task UpdateSubjectAsync(Subject subject, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task DeleteSubjectAsync(int subjectId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private sealed class MockExamMarkRepository(List<ExamMark> marks) : IExamMarkRepository
    {
        public Task<IReadOnlyList<ExamMark>> GetMarksByClassAndSubjectAsync(int classId, int subjectId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<ExamMark>> GetMarksByClassSubjectAndYearAsync(int classId, int subjectId, int academicYearId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<ExamMark>> GetMarksByStudentAsync(int studentId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<ExamMark>>(marks);
        public Task<IReadOnlyList<ExamMark>> GetMarksByStudentAndYearAsync(int studentId, int academicYearId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<ExamMark>>(marks);
        public Task<IReadOnlyList<ExamMark>> GetMarksByClassAsync(int classId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<ExamMark>>(marks);
        public Task SaveOrUpdateMarkAsync(ExamMark mark, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task SaveOrUpdateMarksBatchAsync(IEnumerable<ExamMark> marks, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private sealed class MockAttendanceService(int absenceDays = 0) : IAttendanceService
    {
        public Task<IReadOnlyList<StudentAttendanceDto>> GetClassAttendanceForDateAsync(int classId, DateTime date, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task SaveAttendanceBatchAsync(IEnumerable<SaveAttendanceDto> attendance, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<int> GetStudentAbsenceDaysAsync(int studentId, string academicYear, CancellationToken cancellationToken = default)
            => Task.FromResult(absenceDays);

        public Task<StudentAttendanceSummaryDto?> GetStudentAttendanceSummaryAsync(int studentId, int academicYearId, CancellationToken cancellationToken = default)
            => Task.FromResult<StudentAttendanceSummaryDto?>(new StudentAttendanceSummaryDto { StudentId = studentId, AbsentDays = absenceDays });

        public Task<IReadOnlyList<StudentAttendanceSummaryDto>> GetClassAttendanceSummaryAsync(int classId, int academicYearId, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<IReadOnlyList<MonthlyAttendanceRowDto>> GetMonthlyAttendanceReportAsync(int classId, int year, int month, int academicYearId, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();
    }

    [Fact]
    public async Task GetStudentReportCardData_WhenStudentPassesAll_IsPromotedIsTrue()
    {
        var student = new Student { StudentId = 1, FirstName = "Ahmad", LastName = "Karimi", FatherName = "Mohammad", ClassId = 1, RollNumber = "101" };
        var schoolClass = new SchoolClass { ClassId = 1, GradeName = "صنف هفتم (Grade 7)", NumberOfSubjects = 2 };
        var subjects = new List<Subject>
        {
            new() { SubjectId = 1, ClassId = 1, SubjectName = "ریاضی" },
            new() { SubjectId = 2, ClassId = 1, SubjectName = "فزیک" }
        };
        var marks = new List<ExamMark>
        {
            new() { StudentId = 1, SubjectId = 1, MidtermScore = 35m, FinalScore = 50m },
            new() { StudentId = 1, SubjectId = 2, MidtermScore = 30m, FinalScore = 40m }
        };

        var mockPdf = new MockPdfGenerator();
        var service = new ReportCardService(
            new MockStudentRepository(student),
            new MockClassSubjectRepository(schoolClass, subjects),
            new MockExamMarkRepository(marks),
            mockPdf,
            new MockAttendanceService(0));

        var result = await service.GetStudentReportCardDataAsync(1, "۱۴۰۳");

        Assert.Equal("Ahmad", result.FirstName);
        Assert.Equal(2, result.SubjectMarks.Count);
        Assert.Equal(155m, result.TotalObtainedScore);
        Assert.Equal(200m, result.TotalMaxScore);
        Assert.Equal(77.5m, result.AveragePercentage);
        Assert.Equal(2, result.PassedSubjectsCount);
        Assert.Equal(0, result.FailedSubjectsCount);
        Assert.Equal(PromotionOutcome.Promoted, result.PromotionOutcome);
        Assert.Null(result.FailureReason);
        Assert.Equal(0, result.AbsenceDays);
    }

    [Fact]
    public async Task GetStudentReportCardData_WhenStudentFailsMoreThan3_IsPromotedIsFalse()
    {
        var student = new Student { StudentId = 1, FirstName = "Mahmood", LastName = "Rahimi", FatherName = "Ali", ClassId = 1, RollNumber = "102" };
        var schoolClass = new SchoolClass { ClassId = 1, GradeName = "صنف هشتم", NumberOfSubjects = 4 };
        var subjects = new List<Subject>
        {
            new() { SubjectId = 1, ClassId = 1, SubjectName = "ریاضی" },
            new() { SubjectId = 2, ClassId = 1, SubjectName = "فزیک" },
            new() { SubjectId = 3, ClassId = 1, SubjectName = "کیمیا" },
            new() { SubjectId = 4, ClassId = 1, SubjectName = "بیولوژی" }
        };
        var marks = new List<ExamMark>
        {
            new() { StudentId = 1, SubjectId = 1, MidtermScore = 10m, FinalScore = 20m },
            new() { StudentId = 1, SubjectId = 2, MidtermScore = 12m, FinalScore = 15m },
            new() { StudentId = 1, SubjectId = 3, MidtermScore = 10m, FinalScore = 25m },
            new() { StudentId = 1, SubjectId = 4, MidtermScore = 10m, FinalScore = 20m }
        };

        var mockPdf = new MockPdfGenerator();
        var service = new ReportCardService(
            new MockStudentRepository(student),
            new MockClassSubjectRepository(schoolClass, subjects),
            new MockExamMarkRepository(marks),
            mockPdf,
            new MockAttendanceService(0));

        var result = await service.GetStudentReportCardDataAsync(1, "۱۴۰۳");

        Assert.Equal(4, result.FailedSubjectsCount);
        Assert.Equal(PromotionOutcome.Repeat, result.PromotionOutcome);
        Assert.NotNull(result.FailureReason);
    }

    [Fact]
    public async Task GenerateStudentReportCardPdf_CallsPdfGeneratorWithTemplate()
    {
        var student = new Student { StudentId = 1, FirstName = "Ahmad", LastName = "Karimi", FatherName = "Mohammad", ClassId = 1, RollNumber = "101" };
        var schoolClass = new SchoolClass { ClassId = 1, GradeName = "صنف هفتم", NumberOfSubjects = 1 };
        var subjects = new List<Subject> { new() { SubjectId = 1, ClassId = 1, SubjectName = "دری" } };
        var marks = new List<ExamMark> { new() { StudentId = 1, SubjectId = 1, MidtermScore = 30m, FinalScore = 50m } };

        var mockPdf = new MockPdfGenerator();
        var service = new ReportCardService(
            new MockStudentRepository(student),
            new MockClassSubjectRepository(schoolClass, subjects),
            new MockExamMarkRepository(marks),
            mockPdf,
            new MockAttendanceService(0));

        var tempDir = Path.Combine(Path.GetTempPath(), "MaktabTests_" + Guid.NewGuid());
        var path = await service.GenerateStudentReportCardPdfAsync(1, "۱۴۰۳", tempDir, ReportCardTemplateType.Detailed);

        Assert.Single(mockPdf.GeneratedPaths);
        Assert.EndsWith(".pdf", path);
        Assert.Equal(ReportCardTemplateType.Detailed, mockPdf.TemplateTypes[0]);
    }
}
