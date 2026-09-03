using Maktab.Application.Abstractions;
using Maktab.Application.Services;
using Maktab.Domain.Entities;
using Maktab.Domain.Enums;
using Maktab.Domain.Rules;

namespace Maktab.Tests;

public class ReportServiceTests
{
    private sealed class InMemoryStudentRepository : IStudentRepository
    {
        public List<Student> Students { get; } = [];

        public Task<IReadOnlyList<Student>> GetStudentsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Student>>(Students);

        public Task<IReadOnlyList<Student>> GetStudentsByClassAsync(int classId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Student>>(Students.Where(s => s.ClassId == classId).ToList());

        public Task<Student?> GetStudentByIdAsync(int studentId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Students.FirstOrDefault(s => s.StudentId == studentId));

        public Task<int> CreateStudentAsync(Student student, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task UpdateStudentAsync(Student student, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task DeleteStudentAsync(int studentId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> ExistsByRollNumberAsync(int classId, string rollNumber, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private sealed class InMemoryClassSubjectRepository : IClassSubjectRepository
    {
        public List<SchoolClass> Classes { get; } = [];
        public List<Subject> Subjects { get; } = [];

        public Task<IReadOnlyList<SchoolClass>> GetClassesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SchoolClass>>(Classes);

        public Task<IReadOnlyList<Subject>> GetSubjectsByClassAsync(int classId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Subject>>(Subjects.Where(s => s.ClassId == classId).ToList());

        public Task<int> CreateClassAsync(SchoolClass schoolClass, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task UpdateClassAsync(SchoolClass schoolClass, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task DeleteClassAsync(int classId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<int> CreateSubjectAsync(Subject subject, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task UpdateSubjectAsync(Subject subject, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task DeleteSubjectAsync(int subjectId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<Subject>> GetAllSubjectsAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Subject>>(Subjects);
    }

    private sealed class InMemoryExamMarkRepository : IExamMarkRepository
    {
        public List<ExamMark> Marks { get; } = [];

        public Task<IReadOnlyList<ExamMark>> GetMarksByClassAndSubjectAsync(int classId, int subjectId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ExamMark>>(Marks.Where(m => m.SubjectId == subjectId).ToList());

        public Task<IReadOnlyList<ExamMark>> GetMarksByClassSubjectAndYearAsync(int classId, int subjectId, int academicYearId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ExamMark>>(Marks.Where(m => m.SubjectId == subjectId && m.AcademicYearId == academicYearId).ToList());

        public Task<IReadOnlyList<ExamMark>> GetMarksByStudentAsync(int studentId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ExamMark>>(Marks.Where(m => m.StudentId == studentId).ToList());

        public Task<IReadOnlyList<ExamMark>> GetMarksByStudentAndYearAsync(int studentId, int academicYearId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ExamMark>>(Marks.Where(m => m.StudentId == studentId && m.AcademicYearId == academicYearId).ToList());

        public Task<IReadOnlyList<ExamMark>> GetMarksByClassAsync(int classId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task SaveOrUpdateMarkAsync(ExamMark mark, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task SaveOrUpdateMarksBatchAsync(IEnumerable<ExamMark> marks, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private sealed class InMemoryAttendanceRepository : IAttendanceRepository
    {
        public List<AttendanceRecord> Records { get; } = [];

        public Task<IReadOnlyList<AttendanceRecord>> GetByClassAndDateAsync(int classId, DateTime date, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<AttendanceRecord>> GetByStudentAndRangeAsync(int studentId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<AttendanceRecord>>(Records.Where(r => r.StudentId == studentId).ToList());
        public Task SaveOrUpdateBatchAsync(IEnumerable<AttendanceRecord> records, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<int> GetAbsenceDaysByStudentAndRangeAsync(int studentId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<int> GetAbsenceDaysByStudentAndYearAsync(int studentId, int academicYearId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Records.Count(r => r.StudentId == studentId && r.AcademicYearId == academicYearId && r.Status == AttendanceStatus.Absent));
        public Task<IReadOnlyList<AttendanceRecord>> GetByStudentAndYearAsync(int studentId, int academicYearId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<AttendanceRecord>> GetByClassAndYearAsync(int classId, int academicYearId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private sealed class InMemoryFeeRepository : IFeeRepository
    {
        public List<FeeDto> Fees { get; } = [];

        public Task<IReadOnlyList<FeeDto>> GetFeesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<FeeDto>>(Fees);

        public Task<Fee?> GetFeeByIdAsync(int feeId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<int> CreateFeeAsync(Fee fee, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task DeleteFeeAsync(int feeId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<FeePaymentDto>> GetPaymentsAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<decimal> GetTotalPaidByFeeAsync(int feeId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<int> RecordPaymentAsync(FeePayment payment, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private sealed class InMemoryAcademicYearRepository : IAcademicYearRepository
    {
        public List<AcademicYear> Years { get; } = [];

        public Task<AcademicYear?> GetActiveAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(Years.FirstOrDefault(y => y.IsActive));

        public Task<AcademicYear?> GetByIdAsync(int academicYearId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Years.FirstOrDefault(y => y.AcademicYearId == academicYearId));

        public Task<IReadOnlyList<AcademicYear>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<AcademicYear>>(Years);

        public Task<int> CreateAsync(AcademicYear academicYear, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task SetActiveAsync(int academicYearId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    [Fact]
    public async Task GetClassPerformance_ReturnsCorrectAverages()
    {
        var studentRepo = new InMemoryStudentRepository();
        var classRepo = new InMemoryClassSubjectRepository();
        var markRepo = new InMemoryExamMarkRepository();
        var attendanceRepo = new InMemoryAttendanceRepository();
        var feeRepo = new InMemoryFeeRepository();
        var yearRepo = new InMemoryAcademicYearRepository();

        yearRepo.Years.Add(new AcademicYear { AcademicYearId = 1, YearName = "۱۴۰۴ - ۱۴۰۵", IsActive = true });
        classRepo.Classes.Add(new SchoolClass { ClassId = 1, GradeName = "Grade 1", NumberOfSubjects = 2 });
        classRepo.Subjects.Add(new Subject { SubjectId = 1, ClassId = 1, SubjectName = "Math" });
        classRepo.Subjects.Add(new Subject { SubjectId = 2, ClassId = 1, SubjectName = "Science" });

        studentRepo.Students.Add(new Student { StudentId = 1, FirstName = "A", LastName = "B", FatherName = "C", ClassId = 1, RollNumber = "1" });
        studentRepo.Students.Add(new Student { StudentId = 2, FirstName = "D", LastName = "E", FatherName = "F", ClassId = 1, RollNumber = "2" });

        markRepo.Marks.Add(new ExamMark { StudentId = 1, SubjectId = 1, MidtermScore = 35m, FinalScore = 50m, AcademicYearId = 1 }); // 85
        markRepo.Marks.Add(new ExamMark { StudentId = 1, SubjectId = 2, MidtermScore = 30m, FinalScore = 40m, AcademicYearId = 1 }); // 70
        markRepo.Marks.Add(new ExamMark { StudentId = 2, SubjectId = 1, MidtermScore = 20m, FinalScore = 30m, AcademicYearId = 1 }); // 50
        markRepo.Marks.Add(new ExamMark { StudentId = 2, SubjectId = 2, MidtermScore = 25m, FinalScore = 35m, AcademicYearId = 1 }); // 60

        var service = new ReportService(studentRepo, classRepo, markRepo, attendanceRepo, feeRepo, yearRepo);
        var result = await service.GetClassPerformanceAsync(1, 1);

        Assert.Equal(2, result.TotalStudents);
        Assert.Equal(2, result.SubjectPerformances.Count);
        // Actually Math scores: 85 and 50 => average 67.5
        Assert.Equal(67.5m, result.SubjectPerformances[0].AverageScore);
    }

    [Fact]
    public async Task GetGradeDistribution_CountsGradesCorrectly()
    {
        var studentRepo = new InMemoryStudentRepository();
        var classRepo = new InMemoryClassSubjectRepository();
        var markRepo = new InMemoryExamMarkRepository();
        var attendanceRepo = new InMemoryAttendanceRepository();
        var feeRepo = new InMemoryFeeRepository();
        var yearRepo = new InMemoryAcademicYearRepository();

        yearRepo.Years.Add(new AcademicYear { AcademicYearId = 1, YearName = "۱۴۰۴ - ۱۴۰۵", IsActive = true });
        classRepo.Classes.Add(new SchoolClass { ClassId = 1, GradeName = "Grade 1", NumberOfSubjects = 2 });
        classRepo.Subjects.Add(new Subject { SubjectId = 1, ClassId = 1, SubjectName = "Math" });
        classRepo.Subjects.Add(new Subject { SubjectId = 2, ClassId = 1, SubjectName = "Science" });

        studentRepo.Students.Add(new Student { StudentId = 1, FirstName = "A", LastName = "B", FatherName = "C", ClassId = 1, RollNumber = "1" });
        markRepo.Marks.Add(new ExamMark { StudentId = 1, SubjectId = 1, MidtermScore = 35m, FinalScore = 50m, AcademicYearId = 1 }); // 85
        markRepo.Marks.Add(new ExamMark { StudentId = 1, SubjectId = 2, MidtermScore = 30m, FinalScore = 40m, AcademicYearId = 1 }); // 70
        // average = (85+70)/2 = 77.5 => C? Actually 77.5 falls in C (75-84.99) => C

        var service = new ReportService(studentRepo, classRepo, markRepo, attendanceRepo, feeRepo, yearRepo);
        var result = await service.GetGradeDistributionAsync(1, 1);

        Assert.Equal(1, result.CountC);
        Assert.Equal(0, result.CountA);
        Assert.Equal(0, result.CountB);
        Assert.Equal(0, result.CountD);
        Assert.Equal(0, result.CountF);
    }
}
