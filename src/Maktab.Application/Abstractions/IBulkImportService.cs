namespace Maktab.Application.Abstractions;

public interface IBulkImportService
{
    Task<BulkImportResultDto> ImportStudentsFromCsvAsync(string csvText, CancellationToken cancellationToken = default);

    Task<BulkImportResultDto> ImportMarksFromCsvAsync(
        string csvText,
        int classId,
        int subjectId,
        int academicYearId,
        CancellationToken cancellationToken = default);

    Task<BulkImportResultDto> ImportAttendanceFromCsvAsync(
        string csvText,
        int classId,
        int academicYearId,
        CancellationToken cancellationToken = default);
}
