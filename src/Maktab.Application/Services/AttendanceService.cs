using Maktab.Application.Abstractions;
using Maktab.Domain.Entities;
using Maktab.Domain.Enums;
using Maktab.Domain.Rules;

namespace Maktab.Application.Services;

public sealed class AttendanceService(
    IAttendanceRepository attendanceRepository,
    IStudentRepository studentRepository) : IAttendanceService
{
    public async Task<IReadOnlyList<DailyAttendanceRowDto>> GetDailySheetAsync(
        int classId,
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        if (classId <= 0) throw new ArgumentOutOfRangeException(nameof(classId));

        var students = await studentRepository.GetStudentsByClassAsync(classId, cancellationToken);
        var existing = await attendanceRepository.GetByClassAndDateAsync(classId, date, cancellationToken);
        var statusMap = existing.ToDictionary(r => r.StudentId);

        var result = new List<DailyAttendanceRowDto>();
        foreach (var student in students.OrderBy(s => s.RollNumber))
        {
            statusMap.TryGetValue(student.StudentId, out var record);
            result.Add(new DailyAttendanceRowDto
            {
                StudentId = student.StudentId,
                FirstName = student.FirstName,
                LastName = student.LastName,
                FatherName = student.FatherName,
                RollNumber = student.RollNumber,
                // Default-present pattern: most students are present on a normal day,
                // so the operator only changes the few exceptions.
                Status = record?.Status ?? AttendanceStatus.Present,
                Notes = record?.Notes,
                IsSaved = record is not null
            });
        }

        return result;
    }

    public async Task SaveDailySheetAsync(
        IEnumerable<SaveAttendanceDto> records,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(records);

        var today = DateOnly.FromDateTime(DateTime.Now);
        var domainRecords = new List<AttendanceRecord>();

        foreach (var dto in records)
        {
            if (dto.StudentId <= 0) throw new ArgumentOutOfRangeException(nameof(dto.StudentId));
            if (dto.Date > today)
            {
                throw new ArgumentOutOfRangeException(nameof(dto.Date), "حاضری برای تاریخ آینده قابل ثبت نیست.");
            }

            var student = await studentRepository.GetStudentByIdAsync(dto.StudentId, cancellationToken);
            if (student is null)
            {
                throw new InvalidOperationException($"شاگرد با آیدی {dto.StudentId} یافت نشد.");
            }

            domainRecords.Add(new AttendanceRecord
            {
                StudentId = dto.StudentId,
                Date = dto.Date,
                Status = dto.Status,
                Notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim()
            });
        }

        await attendanceRepository.SaveBatchAsync(domainRecords, cancellationToken);
    }

    public async Task<StudentAttendanceSummaryDto> GetStudentSummaryAsync(
        int studentId,
        CancellationToken cancellationToken = default)
    {
        if (studentId <= 0) throw new ArgumentOutOfRangeException(nameof(studentId));

        var student = await studentRepository.GetStudentByIdAsync(studentId, cancellationToken);
        if (student is null)
        {
            throw new InvalidOperationException($"شاگرد با آیدی {studentId} یافت نشد.");
        }

        var records = await attendanceRepository.GetByStudentAsync(studentId, cancellationToken);
        return BuildSummary(student, records);
    }

    public async Task<IReadOnlyList<StudentAttendanceSummaryDto>> GetClassSummaryAsync(
        int classId,
        CancellationToken cancellationToken = default)
    {
        if (classId <= 0) throw new ArgumentOutOfRangeException(nameof(classId));

        var students = await studentRepository.GetStudentsByClassAsync(classId, cancellationToken);
        var result = new List<StudentAttendanceSummaryDto>();

        foreach (var student in students.OrderBy(s => s.RollNumber))
        {
            var records = await attendanceRepository.GetByStudentAsync(student.StudentId, cancellationToken);
            result.Add(BuildSummary(student, records));
        }

        return result;
    }

    public Task<int> GetAbsenceDaysForPromotionAsync(int studentId, CancellationToken cancellationToken = default)
    {
        if (studentId <= 0) throw new ArgumentOutOfRangeException(nameof(studentId));
        return attendanceRepository.GetAbsenceCountAsync(studentId, cancellationToken);
    }

    private static StudentAttendanceSummaryDto BuildSummary(Student student, IReadOnlyList<AttendanceRecord> records)
    {
        var summary = new StudentAttendanceSummaryDto
        {
            StudentId = student.StudentId,
            FirstName = student.FirstName,
            LastName = student.LastName,
            RollNumber = student.RollNumber,
            PresentDays = records.Count(r => r.Status == AttendanceStatus.Present),
            AbsentDays = records.Count(r => r.Status == AttendanceStatus.Absent),
            IllDays = records.Count(r => r.Status == AttendanceStatus.Ill),
            PermissionDays = records.Count(r => r.Status == AttendanceStatus.Permission)
        };

        summary.ExceedsAbsenceLimit = summary.AbsentDays > PromotionPolicy.MaxAllowedAbsenceDays;
        return summary;
    }
}
