using Maktab.Application.Abstractions;
using Maktab.Domain.Entities;
using Maktab.Domain.Enums;

namespace Maktab.Application.Services;

public sealed class AttendanceService(
    IAttendanceRepository attendanceRepository,
    IStudentRepository studentRepository) : IAttendanceService
{
    public async Task<IReadOnlyList<StudentAttendanceDto>> GetClassAttendanceForDateAsync(
        int classId,
        DateTime date,
        CancellationToken cancellationToken = default)
    {
        if (classId <= 0) throw new ArgumentOutOfRangeException(nameof(classId));

        var students = await studentRepository.GetStudentsByClassAsync(classId, cancellationToken);
        var existingRecords = await attendanceRepository.GetByClassAndDateAsync(classId, date.Date, cancellationToken);
        var recordMap = existingRecords.ToDictionary(r => r.StudentId);

        var result = new List<StudentAttendanceDto>();
        foreach (var student in students.OrderBy(s => s.RollNumber))
        {
            var status = recordMap.TryGetValue(student.StudentId, out var record)
                ? record.Status
                : AttendanceStatus.Present;

            result.Add(new StudentAttendanceDto
            {
                StudentId = student.StudentId,
                FirstName = student.FirstName,
                LastName = student.LastName,
                FatherName = student.FatherName,
                RollNumber = student.RollNumber,
                Status = status
            });
        }

        return result;
    }

    public async Task SaveAttendanceBatchAsync(
        IEnumerable<SaveAttendanceDto> attendance,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(attendance);

        var records = new List<AttendanceRecord>();
        foreach (var item in attendance)
        {
            if (item.StudentId <= 0) throw new ArgumentOutOfRangeException(nameof(item.StudentId));
            if (item.Date == default) throw new ArgumentException("تاریخ نامعتبر است.");

            records.Add(new AttendanceRecord
            {
                StudentId = item.StudentId,
                Date = item.Date.Date,
                Status = item.Status,
                AcademicYearId = item.AcademicYearId
            });
        }

        await attendanceRepository.SaveOrUpdateBatchAsync(records, cancellationToken);
    }

    public async Task<int> GetStudentAbsenceDaysAsync(
        int studentId,
        string academicYear,
        CancellationToken cancellationToken = default)
    {
        if (studentId <= 0) throw new ArgumentOutOfRangeException(nameof(studentId));
        if (string.IsNullOrWhiteSpace(academicYear))
            academicYear = AcademicYearProvider.GetCurrentAcademicYear();

        var (start, end) = ShamsiDateHelper.GetAcademicYearRange(academicYear);
        return await attendanceRepository.GetAbsenceDaysByStudentAndRangeAsync(
            studentId,
            start,
            end,
            cancellationToken);
    }

    public async Task<StudentAttendanceSummaryDto?> GetStudentAttendanceSummaryAsync(
        int studentId,
        int academicYearId,
        CancellationToken cancellationToken = default)
    {
        if (studentId <= 0) throw new ArgumentOutOfRangeException(nameof(studentId));
        if (academicYearId <= 0) throw new ArgumentOutOfRangeException(nameof(academicYearId));

        var student = await studentRepository.GetStudentByIdAsync(studentId, cancellationToken);
        if (student is null) return null;

        var records = await attendanceRepository.GetByStudentAndYearAsync(studentId, academicYearId, cancellationToken);
        return CreateSummary(student, records);
    }

    public async Task<IReadOnlyList<StudentAttendanceSummaryDto>> GetClassAttendanceSummaryAsync(
        int classId,
        int academicYearId,
        CancellationToken cancellationToken = default)
    {
        if (classId <= 0) throw new ArgumentOutOfRangeException(nameof(classId));
        if (academicYearId <= 0) throw new ArgumentOutOfRangeException(nameof(academicYearId));

        var students = await studentRepository.GetStudentsByClassAsync(classId, cancellationToken);
        var records = await attendanceRepository.GetByClassAndYearAsync(classId, academicYearId, cancellationToken);
        var recordsByStudent = records.GroupBy(r => r.StudentId).ToDictionary(g => g.Key, g => g.ToList());

        var result = new List<StudentAttendanceSummaryDto>();
        foreach (var student in students.OrderBy(s => s.RollNumber))
        {
            var studentRecords = recordsByStudent.TryGetValue(student.StudentId, out var list) ? list : [];
            result.Add(CreateSummary(student, studentRecords));
        }

        return result;
    }

    public async Task<IReadOnlyList<MonthlyAttendanceRowDto>> GetMonthlyAttendanceReportAsync(
        int classId,
        int year,
        int month,
        int academicYearId,
        CancellationToken cancellationToken = default)
    {
        if (classId <= 0) throw new ArgumentOutOfRangeException(nameof(classId));
        if (academicYearId <= 0) throw new ArgumentOutOfRangeException(nameof(academicYearId));
        if (month is < 1 or > 12) throw new ArgumentOutOfRangeException(nameof(month));

        var students = await studentRepository.GetStudentsByClassAsync(classId, cancellationToken);
        var records = await attendanceRepository.GetByClassAndYearAsync(classId, academicYearId, cancellationToken);

        var startDate = new DateTime(year, month, 1);
        var daysInMonth = DateTime.DaysInMonth(year, month);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        var recordsByStudent = records
            .Where(r => r.Date.Date >= startDate && r.Date.Date <= endDate)
            .GroupBy(r => r.StudentId)
            .ToDictionary(g => g.Key, g => g.ToDictionary(r => r.Date.Day, r => r.Status));

        var result = new List<MonthlyAttendanceRowDto>();
        foreach (var student in students.OrderBy(s => s.RollNumber))
        {
            var row = new MonthlyAttendanceRowDto
            {
                StudentId = student.StudentId,
                StudentName = $"{student.FirstName} {student.LastName}",
                RollNumber = student.RollNumber
            };

            if (recordsByStudent.TryGetValue(student.StudentId, out var dayMap))
            {
                for (int day = 1; day <= daysInMonth; day++)
                {
                    row.DayStatuses[day] = dayMap.TryGetValue(day, out var status)
                        ? status
                        : AttendanceStatus.Present; // default present if no record
                }
            }
            else
            {
                for (int day = 1; day <= daysInMonth; day++)
                {
                    row.DayStatuses[day] = AttendanceStatus.Present;
                }
            }

            result.Add(row);
        }

        return result;
    }

    private static StudentAttendanceSummaryDto CreateSummary(Student student, IEnumerable<AttendanceRecord> records)
    {
        var list = records.ToList();
        return new StudentAttendanceSummaryDto
        {
            StudentId = student.StudentId,
            StudentName = $"{student.FirstName} {student.LastName}",
            RollNumber = student.RollNumber,
            PresentDays = list.Count(r => r.Status == AttendanceStatus.Present),
            AbsentDays = list.Count(r => r.Status == AttendanceStatus.Absent),
            IllDays = list.Count(r => r.Status == AttendanceStatus.Ill),
            PermissionDays = list.Count(r => r.Status == AttendanceStatus.Permission)
        };
    }
}
