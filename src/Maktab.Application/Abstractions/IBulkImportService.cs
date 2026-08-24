namespace Maktab.Application.Abstractions;

public interface IBulkImportService
{
    // Existing CSV methods (kept for compatibility)
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

    // New file-based methods (auto-detect CSV or Excel)
    Task<BulkImportResultDto> ImportStudentsFromFileAsync(string filePath, CancellationToken cancellationToken = default);

    Task<BulkImportResultDto> ImportMarksFromFileAsync(
        string filePath,
        int classId,
        int subjectId,
        int academicYearId,
        CancellationToken cancellationToken = default);

    Task<BulkImportResultDto> ImportAttendanceFromFileAsync(
        string filePath,
        int classId,
        int academicYearId,
        CancellationToken cancellationToken = default);
}
