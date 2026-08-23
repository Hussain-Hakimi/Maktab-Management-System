using Maktab.Domain.Entities;

namespace Maktab.Application.Abstractions;

public interface IAttendanceRepository
{
    Task<IReadOnlyList<AttendanceRecord>> GetByClassAndDateAsync(int classId, DateTime date, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AttendanceRecord>> GetByStudentAndRangeAsync(int studentId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task SaveOrUpdateBatchAsync(IEnumerable<AttendanceRecord> records, CancellationToken cancellationToken = default);
    Task<int> GetAbsenceDaysByStudentAndRangeAsync(int studentId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task<int> GetAbsenceDaysByStudentAndYearAsync(int studentId, int academicYearId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AttendanceRecord>> GetByStudentAndYearAsync(int studentId, int academicYearId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AttendanceRecord>> GetByClassAndYearAsync(int classId, int academicYearId, CancellationToken cancellationToken = default);
}
