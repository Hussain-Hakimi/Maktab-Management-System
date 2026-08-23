using Maktab.Application.Abstractions;
using Maktab.Application.Services;
using Maktab.Domain.Entities;
using Maktab.Domain.Enums;

namespace Maktab.Tests;

public class AttendanceSummaryTests
{
    private sealed class InMemoryAttendanceRepository : IAttendanceRepository
    {
        public List<AttendanceRecord> Records { get; } = [];

        public Task<IReadOnlyList<AttendanceRecord>> GetByClassAndDateAsync(int classId, DateTime date, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<AttendanceRecord>>(Records.Where(r => r.Date.Date == date.Date).ToList());

        public Task<IReadOnlyList<AttendanceRecord>> GetByStudentAndRangeAsync(int studentId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<AttendanceRecord>>(Records.Where(r => r.StudentId == studentId && r.Date.Date >= startDate.Date && r.Date.Date <= endDate.Date).ToList());

        public Task SaveOrUpdateBatchAsync(IEnumerable<AttendanceRecord> records, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<int> GetAbsenceDaysByStudentAndRangeAsync(int studentId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default) =>
            Task.FromResult(Records.Count(r => r.StudentId == studentId && r.Status == AttendanceStatus.Absent && r.Date.Date >= startDate.Date && r.Date.Date <= endDate.Date));

        public Task<int> GetAbsenceDaysByStudentAndYearAsync(int studentId, int academicYearId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Records.Count(r => r.StudentId == studentId && r.AcademicYearId == academicYearId && r.Status == AttendanceStatus.Absent));

        public Task<IReadOnlyList<AttendanceRecord>> GetByStudentAndYearAsync(int studentId, int academicYearId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<AttendanceRecord>>(Records.Where(r => r.StudentId == studentId && r.AcademicYearId == academicYearId).ToList());

        public Task<IReadOnlyList<AttendanceRecord>> GetByClassAndYearAsync(int classId, int academicYearId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<AttendanceRecord>>(Records.Where(r => r.AcademicYearId == academicYearId).ToList());
    }

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

    [Fact]
    public async Task GetStudentAttendanceSummary_CalculatesCountsCorrectly()
    {
        var attendanceRepo = new InMemoryAttendanceRepository();
        var studentRepo = new InMemoryStudentRepository();

        var student = new Student { StudentId = 1, FirstName = "Ahmad", LastName = "Karimi", FatherName = "Mohammad", ClassId = 1, RollNumber = "101" };
        studentRepo.Students.Add(student);

        attendanceRepo.Records.Add(new AttendanceRecord { StudentId = 1, Date = new DateTime(2024, 1, 1), Status = AttendanceStatus.Present, AcademicYearId = 1 });
        attendanceRepo.Records.Add(new AttendanceRecord { StudentId = 1, Date = new DateTime(2024, 1, 2), Status = AttendanceStatus.Absent, AcademicYearId = 1 });
        attendanceRepo.Records.Add(new AttendanceRecord { StudentId = 1, Date = new DateTime(2024, 1, 3), Status = AttendanceStatus.Ill, AcademicYearId = 1 });
        attendanceRepo.Records.Add(new AttendanceRecord { StudentId = 1, Date = new DateTime(2024, 1, 4), Status = AttendanceStatus.Permission, AcademicYearId = 1 });

        var service = new AttendanceService(attendanceRepo, studentRepo);
        var summary = await service.GetStudentAttendanceSummaryAsync(1, 1);

        Assert.NotNull(summary);
        Assert.Equal(1, summary.PresentDays);
        Assert.Equal(1, summary.AbsentDays);
        Assert.Equal(1, summary.IllDays);
        Assert.Equal(1, summary.PermissionDays);
        Assert.Equal(4, summary.TotalDays);
        Assert.Equal(25m, summary.AbsenceRate);
    }

    [Fact]
    public async Task GetClassAttendanceSummary_ReturnsAllStudents()
    {
        var attendanceRepo = new InMemoryAttendanceRepository();
        var studentRepo = new InMemoryStudentRepository();

        studentRepo.Students.Add(new Student { StudentId = 1, FirstName = "A", LastName = "B", FatherName = "C", ClassId = 1, RollNumber = "1" });
        studentRepo.Students.Add(new Student { StudentId = 2, FirstName = "D", LastName = "E", FatherName = "F", ClassId = 1, RollNumber = "2" });

        attendanceRepo.Records.Add(new AttendanceRecord { StudentId = 1, Date = new DateTime(2024, 1, 1), Status = AttendanceStatus.Present, AcademicYearId = 1 });
        attendanceRepo.Records.Add(new AttendanceRecord { StudentId = 2, Date = new DateTime(2024, 1, 1), Status = AttendanceStatus.Absent, AcademicYearId = 1 });

        var service = new AttendanceService(attendanceRepo, studentRepo);
        var result = await service.GetClassAttendanceSummaryAsync(1, 1);

        Assert.Equal(2, result.Count);
        Assert.Equal(1, result[0].PresentDays);
        Assert.Equal(0, result[1].PresentDays);
        Assert.Equal(1, result[1].AbsentDays);
    }
}
