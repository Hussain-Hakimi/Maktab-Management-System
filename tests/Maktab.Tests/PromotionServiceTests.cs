using Maktab.Application.Abstractions;
using Maktab.Application.Services;
using Maktab.Domain.Entities;
using Maktab.Domain.Enums;

namespace Maktab.Tests;

public class PromotionServiceTests
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

        public Task UpdateStudentAsync(Student student, CancellationToken cancellationToken = default)
        {
            var idx = Students.FindIndex(s => s.StudentId == student.StudentId);
            if (idx >= 0) Students[idx] = student;
            return Task.CompletedTask;
        }

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
        public Task<IReadOnlyList<AttendanceRecord>> GetByStudentAndRangeAsync(int studentId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task SaveOrUpdateBatchAsync(IEnumerable<AttendanceRecord> records, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<int> GetAbsenceDaysByStudentAndRangeAsync(int studentId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<int> GetAbsenceDaysByStudentAndYearAsync(int studentId, int academicYearId, CancellationToken cancellationToken = default)
            => Task.FromResult(Records.Count(r => r.StudentId == studentId && r.AcademicYearId == academicYearId && r.Status == AttendanceStatus.Absent));

        public Task<IReadOnlyList<AttendanceRecord>> GetByStudentAndYearAsync(int studentId, int academicYearId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<AttendanceRecord>>(Records.Where(r => r.StudentId == studentId && r.AcademicYearId == academicYearId).ToList());

        public Task<IReadOnlyList<AttendanceRecord>> GetByClassAndYearAsync(int classId, int academicYearId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<AttendanceRecord>>(Records.Where(r => r.AcademicYearId == academicYearId).ToList());
    }

    private sealed class InMemoryPromotionHistoryRepository : IStudentPromotionHistoryRepository
    {
        public List<StudentPromotionHistory> Histories { get; } = [];

        public Task<int> AddAsync(StudentPromotionHistory history, CancellationToken cancellationToken = default)
        {
            history.PromotionId = Histories.Count + 1;
            Histories.Add(history);
            return Task.FromResult(history.PromotionId);
        }

        public Task<IReadOnlyList<StudentPromotionHistory>> GetByStudentAsync(int studentId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<StudentPromotionHistory>>(Histories.Where(h => h.StudentId == studentId).ToList());

        public Task<IReadOnlyList<PromotionHistoryDto>> GetHistoryAsync(int? academicYearId, int? studentId, CancellationToken cancellationToken = default)
        {
            var query = Histories.AsEnumerable();

            if (academicYearId.HasValue)
                query = query.Where(h => h.AcademicYearId == academicYearId.Value);

            if (studentId.HasValue)
                query = query.Where(h => h.StudentId == studentId.Value);

            var result = query.Select(h => new PromotionHistoryDto
            {
                PromotionId = h.PromotionId,
                StudentId = h.StudentId,
                StudentName = h.StudentId.ToString(),
                RollNumber = h.StudentId.ToString(),
                FromClassName = h.FromClassId.ToString(),
                ToClassName = h.ToClassId?.ToString(),
                AcademicYearName = h.AcademicYearId.ToString(),
                Result = h.Result,
                PromotionDate = h.PromotionDate
            }).ToList();

            return Task.FromResult<IReadOnlyList<PromotionHistoryDto>>(result);
        }
    }

    [Fact]
    public async Task RunPromotion_WhenStudentPassesAll_UpdatesClassAndRecordsHistory()
    {
        var studentRepo = new InMemoryStudentRepository();
        var classRepo = new InMemoryClassSubjectRepository();
        var markRepo = new InMemoryExamMarkRepository();
        var attendanceRepo = new InMemoryAttendanceRepository();
        var historyRepo = new InMemoryPromotionHistoryRepository();

        classRepo.Classes.Add(new SchoolClass { ClassId = 1, GradeName = "Grade 1", NumberOfSubjects = 2 });
        classRepo.Classes.Add(new SchoolClass { ClassId = 2, GradeName = "Grade 2", NumberOfSubjects = 2 });
        classRepo.Subjects.Add(new Subject { SubjectId = 1, ClassId = 1, SubjectName = "Math" });
        classRepo.Subjects.Add(new Subject { SubjectId = 2, ClassId = 1, SubjectName = "Science" });

        var student = new Student
        {
            StudentId = 1,
            FirstName = "Ahmad",
            LastName = "Karimi",
            FatherName = "Mohammad",
            ClassId = 1,
            RollNumber = "101",
            RegistrationDate = DateTime.Now
        };
        studentRepo.Students.Add(student);

        markRepo.Marks.Add(new ExamMark { StudentId = 1, SubjectId = 1, MidtermScore = 35m, FinalScore = 50m, AcademicYearId = 1 });
        markRepo.Marks.Add(new ExamMark { StudentId = 1, SubjectId = 2, MidtermScore = 30m, FinalScore = 45m, AcademicYearId = 1 });

        var service = new PromotionService(studentRepo, classRepo, markRepo, attendanceRepo, historyRepo);
        var result = await service.RunPromotionForYearAsync(1);

        Assert.Equal(2, result.TotalStudents);
        Assert.Equal(1, result.PromotedCount);
        Assert.Equal(2, studentRepo.Students[0].ClassId);
        Assert.Equal(2, historyRepo.Histories.Count);
        Assert.Equal("Promoted", historyRepo.Histories[0].Result);
    }

    [Fact]
    public async Task GetPromotionHistory_ReturnsCorrectRecords()
    {
        var studentRepo = new InMemoryStudentRepository();
        var classRepo = new InMemoryClassSubjectRepository();
        var markRepo = new InMemoryExamMarkRepository();
        var attendanceRepo = new InMemoryAttendanceRepository();
        var historyRepo = new InMemoryPromotionHistoryRepository();

        historyRepo.Histories.Add(new StudentPromotionHistory
        {
            StudentId = 1,
            FromClassId = 1,
            ToClassId = 2,
            AcademicYearId = 1,
            Result = "Promoted",
            PromotionDate = DateTime.Now
        });

        historyRepo.Histories.Add(new StudentPromotionHistory
        {
            StudentId = 2,
            FromClassId = 1,
            ToClassId = null,
            AcademicYearId = 1,
            Result = "Repeat",
            PromotionDate = DateTime.Now
        });

        var service = new PromotionService(studentRepo, classRepo, markRepo, attendanceRepo, historyRepo);

        var all = await service.GetPromotionHistoryAsync();
        Assert.Equal(2, all.Count);

        var year1 = await service.GetPromotionHistoryAsync(academicYearId: 1);
        Assert.Equal(2, year1.Count);

        var student1 = await service.GetPromotionHistoryAsync(studentId: 1);
        Assert.Single(student1);
        Assert.Equal("Promoted", student1[0].Result);
    }
}
