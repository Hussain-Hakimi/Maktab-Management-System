using Maktab.Domain.Entities;
using Maktab.Domain.Enums;

namespace Maktab.Application.Abstractions;

public interface IAttendanceRepository
{
    Task<IReadOnlyList<AttendanceRecord>> GetByClassAndDateAsync(int classId, DateOnly date, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AttendanceRecord>> GetByStudentAsync(int studentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AttendanceRecord>> GetByClassAndDateRangeAsync(int classId, DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken = default);
    Task<int> GetStatusCountAsync(int studentId, AttendanceStatus status, CancellationToken cancellationToken = default);
    Task SaveBatchAsync(IEnumerable<AttendanceRecord> records, CancellationToken cancellationToken = default);
}
