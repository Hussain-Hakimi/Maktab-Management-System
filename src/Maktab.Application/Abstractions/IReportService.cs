namespace Maktab.Application.Abstractions;

public interface IReportService
{
    Task<ClassPerformanceReportDto> GetClassPerformanceAsync(int classId, int academicYearId, CancellationToken cancellationToken = default);
    Task<GradeDistributionDto> GetGradeDistributionAsync(int classId, int academicYearId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StudentExportRowDto>> GetStudentExportDataAsync(int classId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MarkExportRowDto>> GetMarkExportDataAsync(int classId, int subjectId, int academicYearId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AttendanceExportRowDto>> GetAttendanceExportDataAsync(int classId, int academicYearId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FeeExportRowDto>> GetFeeExportDataAsync(int classId, int academicYearId, CancellationToken cancellationToken = default);
}
