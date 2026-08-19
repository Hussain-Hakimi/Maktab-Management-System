using Maktab.Application.Abstractions;

namespace Maktab.Application.Abstractions;

public interface IAttendanceService
{
    Task<IReadOnlyList<StudentAttendanceDto>> GetClassAttendanceForDateAsync(
        int classId,
        DateTime date,
        CancellationToken cancellationToken = default);

    Task SaveAttendanceBatchAsync(
        IEnumerable<SaveAttendanceDto> attendance,
        CancellationToken cancellationToken = default);

    Task<int> GetStudentAbsenceDaysAsync(
        int studentId,
        string academicYear,
        CancellationToken cancellationToken = default);
}
