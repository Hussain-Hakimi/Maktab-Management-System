namespace Maktab.Application.Abstractions;

public sealed record BulkImportPreviewResult(
    int TotalRows,
    int ValidRows,
    IReadOnlyList<string> Errors)
{
    public int InvalidRows => TotalRows - ValidRows;
    public bool CanImport => TotalRows > 0 && InvalidRows == 0;
}

public interface IBulkImportPreviewService
{
    Task<BulkImportPreviewResult> PreviewStudentsFromCsvAsync(string csvText, CancellationToken cancellationToken = default);
    Task<BulkImportPreviewResult> PreviewStudentsFromFileAsync(string filePath, CancellationToken cancellationToken = default);

    Task<BulkImportPreviewResult> PreviewMarksFromCsvAsync(string csvText, int classId, int subjectId, int academicYearId, CancellationToken cancellationToken = default);
    Task<BulkImportPreviewResult> PreviewMarksFromFileAsync(string filePath, int classId, int subjectId, int academicYearId, CancellationToken cancellationToken = default);

    Task<BulkImportPreviewResult> PreviewAttendanceFromCsvAsync(string csvText, int classId, int academicYearId, CancellationToken cancellationToken = default);
    Task<BulkImportPreviewResult> PreviewAttendanceFromFileAsync(string filePath, int classId, int academicYearId, CancellationToken cancellationToken = default);

    Task<BulkImportPreviewResult> PreviewMultiSubjectMarksFromFileAsync(string filePath, int classId, int academicYearId, CancellationToken cancellationToken = default);
}
