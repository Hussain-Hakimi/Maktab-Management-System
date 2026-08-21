namespace Maktab.Application.Abstractions;

public interface IBulkImportService
{
    Task<BulkImportResultDto> ImportStudentsFromCsvAsync(string csvText, CancellationToken cancellationToken = default);
}
