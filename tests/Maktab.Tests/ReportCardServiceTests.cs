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
        public List<ReportCardType> ReportTypes { get; } = [];

        public Task GeneratePdfReportAsync(
            StudentReportCardDto reportCard,
            string outputFilePath,
            ReportCardType reportType,
            CancellationToken cancellationToken = default)
        {
            GeneratedPaths.Add(outputFilePath);
            ReportTypes.Add(reportType);
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
        public Task<IReadOnlyList<Subject>> GetAllSubjectsAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Subject>>(subjects);

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
        public Task<IReadOnlyList<ExamMark>> GetMarksByStudentAndYearAsync(int studentId, int academicYearId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ExamMark>>(marks.Where(m => m.StudentId == studentId && m.AcademicYearId == academicYearId).ToList());
        public Task<IReadOnlyList<ExamMark>> GetMarksByClassAsync(int classId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<ExamMark>>(marks);
        public Task SaveOrUpdateMarkAsync(ExamMark mark, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task SaveOrUpdateMarksBatchAsync(IEnumerable<ExamMark> marks, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private sealed class MockAcademicYearRepository(List<AcademicYear> years) : IAcademicYearRepository
    {
        public Task<AcademicYear?> GetActiveAsync(CancellationToken cancellationToken = default) => Task.FromResult(years.FirstOrDefault(y => y.IsActive));
        public Task<AcademicYear?> GetByIdAsync(int academicYearId, CancellationToken cancellationToken = default) => Task.FromResult(years.FirstOrDefault(y => y.AcademicYearId == academicYearId));
        public Task<IReadOnlyList<AcademicYear>> GetAllAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<AcademicYear>>(years);
        public Task<int> CreateAsync(AcademicYear academicYear, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task SetActiveAsync(int academicYearId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
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

    private sealed class MockSchoolSettingsService : ISchoolSettingsService
    {
        public Task<SchoolSettingsDto> GetSettingsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new SchoolSettingsDto
            {
                SchoolName = "مکتب نمونه",
                SchoolAddress = "کابل",
                PhoneNumber = "123456",
                AcademicYear = "۱۴۰۳",
                GovernmentTitle = "امارت اسلامی افغانستان",
                ProvincialEducationHeader = "ریاست معارف کابل",
                DistrictEducationHeader = "مدیریت معارف حوزه سوم"
            });

        public Task SaveSettingsAsync(SchoolSettingsDto settings, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private static MockAcademicYearRepository AcademicYears() => new([
        new AcademicYear { AcademicYearId = 1, YearName = "۱۴۰۳", StartDate = new DateTime(2024, 1, 1), EndDate = new DateTime(2024, 12, 31), IsActive = true },
        new AcademicYear { AcademicYearId = 2, YearName = "۱۴۰۴", StartDate = new DateTime(2025, 1, 1), EndDate = new DateTime(2025, 12, 31), IsActive = false }
    ]);

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
            new() { StudentId = 1, SubjectId = 1, MidtermScore = 35m, FinalScore = 50m, AcademicYearId = 1 },
            new() { StudentId = 1, SubjectId = 2, MidtermScore = 30m, FinalScore = 40m, AcademicYearId = 1 }
        };

        var mockPdf = new MockPdfGenerator();
        var service = new ReportCardService(
            new MockStudentRepository(student),
            new MockClassSubjectRepository(schoolClass, subjects),
            new MockExamMarkRepository(marks),
            mockPdf,
            new MockAttendanceService(0),
            new MockSchoolSettingsService(),
            AcademicYears());

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
    public async Task GetStudentReportCardData_UsesOnlyMarksFromRequestedAcademicYear()
    {
        var student = new Student { StudentId = 1, FirstName = "Ahmad", LastName = "Karimi", FatherName = "Mohammad", ClassId = 1, RollNumber = "101" };
        var schoolClass = new SchoolClass { ClassId = 1, GradeName = "صنف هفتم", NumberOfSubjects = 1 };
        var subjects = new List<Subject> { new() { SubjectId = 1, ClassId = 1, SubjectName = "ریاضی" } };
        var marks = new List<ExamMark>
        {
            new() { StudentId = 1, SubjectId = 1, MidtermScore = 35m, FinalScore = 50m, AcademicYearId = 1 },
            new() { StudentId = 1, SubjectId = 1, MidtermScore = 10m, FinalScore = 20m, AcademicYearId = 2 }
        };

        var service = new ReportCardService(
            new MockStudentRepository(student),
            new MockClassSubjectRepository(schoolClass, subjects),
            new MockExamMarkRepository(marks),
            new MockPdfGenerator(),
            new MockAttendanceService(0),
            new MockSchoolSettingsService(),
            AcademicYears());

        var result = await service.GetStudentReportCardDataAsync(1, "۱۴۰۳");

        Assert.Equal(85m, result.SubjectMarks.Single().TotalScore);
        Assert.Equal(85m, result.TotalObtainedScore);
        Assert.Equal(85m, result.AveragePercentage);
    }

    [Fact]
    public async Task GetStudentReportCardData_WhenAcademicYearDoesNotExist_Throws()
    {
        var student = new Student { StudentId = 1, FirstName = "Ahmad", LastName = "Karimi", FatherName = "Mohammad", ClassId = 1, RollNumber = "101" };
        var subjects = new List<Subject> { new() { SubjectId = 1, ClassId = 1, SubjectName = "دری" } };
        var schoolClass = new SchoolClass { ClassId = 1, GradeName = "صنف هفتم", NumberOfSubjects = 1 };

        var service = new ReportCardService(
            new MockStudentRepository(student),
            new MockClassSubjectRepository(schoolClass, subjects),
            new MockExamMarkRepository([]),
            new MockPdfGenerator(),
            new MockAttendanceService(0),
            new MockSchoolSettingsService(),
            AcademicYears());

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetStudentReportCardDataAsync(1, "۱۴۰۵"));
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
            new() { StudentId = 1, SubjectId = 1, MidtermScore = 10m, FinalScore = 20m, AcademicYearId = 1 },
            new() { StudentId = 1, SubjectId = 2, MidtermScore = 12m, FinalScore = 15m, AcademicYearId = 1 },
            new() { StudentId = 1, SubjectId = 3, MidtermScore = 10m, FinalScore = 25m, AcademicYearId = 1 },
            new() { StudentId = 1, SubjectId = 4, MidtermScore = 10m, FinalScore = 20m, AcademicYearId = 1 }
        };

        var mockPdf = new MockPdfGenerator();
        var service = new ReportCardService(
            new MockStudentRepository(student),
            new MockClassSubjectRepository(schoolClass, subjects),
            new MockExamMarkRepository(marks),
            mockPdf,
            new MockAttendanceService(0),
            new MockSchoolSettingsService(),
            AcademicYears());

        var result = await service.GetStudentReportCardDataAsync(1, "۱۴۰۳");

        Assert.Equal(4, result.FailedSubjectsCount);
        Assert.Equal(PromotionOutcome.Repeat, result.PromotionOutcome);
        Assert.NotNull(result.FailureReason);
    }

    [Fact]
    public async Task GenerateStudentReportCardPdf_CallsPdfGeneratorWithReportType()
    {
        var student = new Student { StudentId = 1, FirstName = "Ahmad", LastName = "Karimi", FatherName = "Mohammad", ClassId = 1, RollNumber = "101" };
        var schoolClass = new SchoolClass { ClassId = 1, GradeName = "صنف هفتم", NumberOfSubjects = 1 };
        var subjects = new List<Subject> { new() { SubjectId = 1, ClassId = 1, SubjectName = "دری" } };
        var marks = new List<ExamMark> { new() { StudentId = 1, SubjectId = 1, MidtermScore = 30m, FinalScore = 50m, AcademicYearId = 1 } };

        var mockPdf = new MockPdfGenerator();
        var service = new ReportCardService(
            new MockStudentRepository(student),
            new MockClassSubjectRepository(schoolClass, subjects),
            new MockExamMarkRepository(marks),
            mockPdf,
            new MockAttendanceService(0),
            new MockSchoolSettingsService(),
            AcademicYears());

        var tempDir = Path.Combine(Path.GetTempPath(), "MaktabTests_" + Guid.NewGuid());
        var path = await service.GenerateStudentReportCardPdfAsync(1, "۱۴۰۳", tempDir, ReportCardType.Annual);

        Assert.Single(mockPdf.GeneratedPaths);
        Assert.EndsWith(".pdf", path);
        Assert.Equal(ReportCardType.Annual, mockPdf.ReportTypes[0]);
    }
}
