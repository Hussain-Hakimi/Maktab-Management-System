namespace Maktab.Application.Abstractions;

public interface IAttendanceService
{
    Task<IReadOnlyList<DailyAttendanceRowDto>> GetDailySheetAsync(int classId, DateOnly date, CancellationToken cancellationToken = default);
    Task SaveDailySheetAsync(IEnumerable<SaveAttendanceDto> records, CancellationToken cancellationToken = default);
    Task<StudentAttendanceSummaryDto> GetStudentSummaryAsync(int studentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StudentAttendanceSummaryDto>> GetClassSummaryAsync(int classId, CancellationToken cancellationToken = default);
    Task<int> GetAbsenceDaysForPromotionAsync(int studentId, CancellationToken cancellationToken = default);
}
