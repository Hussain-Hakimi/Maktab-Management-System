namespace Maktab.Application.Abstractions;

public interface IExportService
{
    Task ExportAsync<T>(IEnumerable<T> data, string filePath, string sheetName);
}
