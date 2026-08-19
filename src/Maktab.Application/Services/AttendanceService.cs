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
                : AttendanceStatus.Present; // default to Present

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
                Status = item.Status
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
}
