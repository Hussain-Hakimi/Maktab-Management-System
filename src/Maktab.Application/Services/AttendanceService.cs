using Maktab.Application.Abstractions;
using Maktab.Domain.Entities;
using Maktab.Domain.Enums;
using Maktab.Domain.Rules;

namespace Maktab.Application.Services;

public sealed class AttendanceService(
    IAttendanceRepository attendanceRepository,
    IStudentRepository studentRepository) : IAttendanceService
{
    public async Task<IReadOnlyList<StudentAttendanceRowDto>> GetDailySheetAsync(
        int classId,
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        if (classId <= 0) throw new ArgumentOutOfRangeException(nameof(classId));

        var students = await studentRepository.GetStudentsByClassAsync(classId, cancellationToken);
        var savedRecords = await attendanceRepository.GetByClassAndDateAsync(classId, date, cancellationToken);
        var savedMap = savedRecords.ToDictionary(r => r.StudentId);

        var rows = new List<StudentAttendanceRowDto>();
        foreach (var student in students.OrderBy(s => s.RollNumber))
        {
            var hasSaved = savedMap.TryGetValue(student.StudentId, out var record);
            rows.Add(new StudentAttendanceRowDto
            {
                StudentId = student.StudentId,
                FirstName = student.FirstName,
                LastName = student.LastName,
                FatherName = student.FatherName,
                RollNumber = student.RollNumber,
                // Default-present pattern: on an unmarked day everyone starts as Present,
                // so the teacher only changes the few exceptions instead of clicking every student.
                Status = hasSaved ? record!.Status : AttendanceStatus.Present,
                Notes = hasSaved ? record!.Notes : null,
                IsSaved = hasSaved
            });
        }

        return rows;
    }

    public async Task SaveDailySheetAsync(
        int classId,
        DateOnly date,
        IEnumerable<SaveAttendanceDto> records,
        CancellationToken cancellationToken = default)
    {
        if (classId <= 0) throw new ArgumentOutOfRangeException(nameof(classId));
        ArgumentNullException.ThrowIfNull(records);

        var list = records.ToList();
        foreach (var r in list)
        {
            if (r.StudentId <= 0) throw new ArgumentOutOfRangeException(nameof(r.StudentId));
            if (r.Date != date)
            {
                throw new ArgumentException("A record date does not match the sheet date.", nameof(records));
            }
        }

        // Safety: every student in the payload must actually belong to this class.
        var students = await studentRepository.GetStudentsByClassAsync(classId, cancellationToken);
        var validIds = students.Select(s => s.StudentId).ToHashSet();
        foreach (var r in list)
        {
            if (!validIds.Contains(r.StudentId))
            {
                throw new InvalidOperationException($"Student {r.StudentId} does not belong to class {classId}.");
            }
        }

        var domainRecords = list.Select(r => new AttendanceRecord
        {
            StudentId = r.StudentId,
            Date = r.Date,
            Status = r.Status,
            Notes = string.IsNullOrWhiteSpace(r.Notes) ? null : r.Notes.Trim()
        });

        await attendanceRepository.SaveBatchAsync(domainRecords, cancellationToken);
    }

    public async Task<IReadOnlyList<StudentAbsenceSummaryDto>> GetClassAbsenceStatisticsAsync(
        int classId,
        CancellationToken cancellationToken = default)
    {
        if (classId <= 0) throw new ArgumentOutOfRangeException(nameof(classId));

        var students = await studentRepository.GetStudentsByClassAsync(classId, cancellationToken);
        var result = new List<StudentAbsenceSummaryDto>();

        foreach (var student in students.OrderBy(s => s.RollNumber))
        {
            var records = await attendanceRepository.GetByStudentAsync(student.StudentId, cancellationToken);
            var summary = new StudentAbsenceSummaryDto
            {
                StudentId = student.StudentId,
                FirstName = student.FirstName,
                LastName = student.LastName,
                FatherName = student.FatherName,
                RollNumber = student.RollNumber,
                PresentDays = records.Count(r => r.Status == AttendanceStatus.Present),
                AbsentDays = records.Count(r => r.Status == AttendanceStatus.Absent),
                IllDays = records.Count(r => r.Status == AttendanceStatus.Ill),
                PermissionDays = records.Count(r => r.Status == AttendanceStatus.Permission)
            };
            summary.ExceedsAbsenceLimit = summary.AbsenceDaysForPromotion > PromotionPolicy.MaxAllowedAbsenceDays;
            result.Add(summary);
        }

        return result;
    }

    public async Task<int> GetAbsenceDaysForPromotionAsync(int studentId, CancellationToken cancellationToken = default)
    {
        if (studentId <= 0) throw new ArgumentOutOfRangeException(nameof(studentId));
        // Only unexcused "Absent" days count toward the 30-day promotion limit;
        // Ill and Permission are excused absences.
        return await attendanceRepository.GetStatusCountAsync(studentId, AttendanceStatus.Absent, cancellationToken);
    }
}
