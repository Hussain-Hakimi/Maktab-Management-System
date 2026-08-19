using Maktab.Application.Abstractions;
using Maktab.Application.Services;
using Maktab.Domain.Entities;
using Maktab.Domain.Enums;

namespace Maktab.Tests;

public class AttendanceServiceTests
{
    private sealed class InMemoryAttendanceRepository : IAttendanceRepository
    {
        private readonly List<AttendanceRecord> _records = [];
        private int _nextId = 1;

        public Task<IReadOnlyList<AttendanceRecord>> GetByClassAndDateAsync(int classId, DateTime date, CancellationToken cancellationToken = default)
        {
            var result = _records.Where(r => r.Date.Date == date.Date && _studentsClassMap.TryGetValue(r.StudentId, out var cid) && cid == classId).ToList();
            return Task.FromResult<IReadOnlyList<AttendanceRecord>>(result);
        }

        public Task<IReadOnlyList<AttendanceRecord>> GetByStudentAndRangeAsync(int studentId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
        {
            var result = _records.Where(r => r.StudentId == studentId && r.Date.Date >= startDate.Date && r.Date.Date <= endDate.Date).ToList();
            return Task.FromResult<IReadOnlyList<AttendanceRecord>>(result);
        }

        public Task SaveOrUpdateBatchAsync(IEnumerable<AttendanceRecord> records, CancellationToken cancellationToken = default)
        {
            foreach (var record in records)
            {
                var existing = _records.FirstOrDefault(r => r.StudentId == record.StudentId && r.Date.Date == record.Date.Date);
                if (existing != null)
                {
                    existing.Status = record.Status;
                }
                else
                {
                    record.AttendanceId = _nextId++;
                    _records.Add(record);
                }
            }
            return Task.CompletedTask;
        }

        public Task<int> GetAbsenceDaysByStudentAndRangeAsync(int studentId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
        {
            var count = _records.Count(r => r.StudentId == studentId && r.Status == AttendanceStatus.Absent && r.Date.Date >= startDate.Date && r.Date.Date <= endDate.Date);
            return Task.FromResult(count);
        }

        private readonly Dictionary<int, int> _studentsClassMap = new()
        {
            { 1, 1 },
            { 2, 1 }
        };
    }

    private sealed class MockStudentRepository(List<Student> students) : IStudentRepository
    {
        public Task<IReadOnlyList<Student>> GetStudentsAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Student>>(students);
        public Task<IReadOnlyList<Student>> GetStudentsByClassAsync(int classId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Student>>(students.Where(s => s.ClassId == classId).ToList());
        public Task<Student?> GetStudentByIdAsync(int studentId, CancellationToken cancellationToken = default) => Task.FromResult(students.FirstOrDefault(s => s.StudentId == studentId));
        public Task<int> CreateStudentAsync(Student student, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task UpdateStudentAsync(Student student, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task DeleteStudentAsync(int studentId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> ExistsByRollNumberAsync(int classId, string rollNumber, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    [Fact]
    public async Task GetClassAttendanceForDate_ReturnsAllStudentsWithDefaultPresent()
    {
        var students = new List<Student>
        {
            new() { StudentId = 1, FirstName = "Ahmad", LastName = "Karimi", FatherName = "Mohammad", ClassId = 1, RollNumber = "101" },
            new() { StudentId = 2, FirstName = "Mahmood", LastName = "Rahimi", FatherName = "Ali", ClassId = 1, RollNumber = "102" }
        };
        var repo = new InMemoryAttendanceRepository();
        var service = new AttendanceService(repo, new MockStudentRepository(students));

        var result = await service.GetClassAttendanceForDateAsync(1, new DateTime(2025, 5, 10));

        Assert.Equal(2, result.Count);
        Assert.All(result, r => Assert.Equal(AttendanceStatus.Present, r.Status));
    }

    [Fact]
    public async Task SaveAttendanceBatch_ThenGetClassAttendance_ReturnsSavedStatus()
    {
        var students = new List<Student>
        {
            new() { StudentId = 1, FirstName = "Ahmad", LastName = "Karimi", FatherName = "Mohammad", ClassId = 1, RollNumber = "101" }
        };
        var repo = new InMemoryAttendanceRepository();
        var service = new AttendanceService(repo, new MockStudentRepository(students));

        var date = new DateTime(2025, 5, 10);
        await service.SaveAttendanceBatchAsync(new[]
        {
            new SaveAttendanceDto(1, date, AttendanceStatus.Absent)
        });

        var result = await service.GetClassAttendanceForDateAsync(1, date);

        Assert.Single(result);
        Assert.Equal(AttendanceStatus.Absent, result[0].Status);
    }

    [Fact]
    public async Task GetStudentAbsenceDays_CalculatesCorrectly()
    {
        var students = new List<Student>
        {
            new() { StudentId = 1, FirstName = "Ahmad", LastName = "Karimi", FatherName = "Mohammad", ClassId = 1, RollNumber = "101" }
        };
        var repo = new InMemoryAttendanceRepository();
        var service = new AttendanceService(repo, new MockStudentRepository(students));

        var yearRange = ShamsiDateHelper.GetAcademicYearRange("۱۴۰۳ - ۱۴۰۴");
        var date1 = yearRange.Start.AddDays(5);
        var date2 = yearRange.Start.AddDays(10);
        var date3 = yearRange.Start.AddDays(15);

        await service.SaveAttendanceBatchAsync(new[]
        {
            new SaveAttendanceDto(1, date1, AttendanceStatus.Absent),
            new SaveAttendanceDto(1, date2, AttendanceStatus.Absent),
            new SaveAttendanceDto(1, date3, AttendanceStatus.Present)
        });

        var absenceDays = await service.GetStudentAbsenceDaysAsync(1, "۱۴۰۳ - ۱۴۰۴");

        Assert.Equal(2, absenceDays);
    }
}
