namespace Maktab.Application.Abstractions;

public interface IAttendanceService
{
    Task<IReadOnlyList<StudentAttendanceRowDto>> GetDailySheetAsync(int classId, DateOnly date, CancellationToken cancellationToken = default);
    Task SaveDailySheetAsync(int classId, DateOnly date, IEnumerable<SaveAttendanceDto> records, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StudentAbsenceSummaryDto>> GetClassAbsenceStatisticsAsync(int classId, CancellationToken cancellationToken = default);
    Task<int> GetAbsenceDaysForPromotionAsync(int studentId, CancellationToken cancellationToken = default);
}
