using ClosedXML.Excel;
using Maktab.Application.Abstractions;

namespace Maktab.Infrastructure.Reports;

public sealed class ExcelExportService : IExportService
{
    public async Task ExportAsync<T>(IEnumerable<T> data, string filePath, string sheetName)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add(sheetName);
        worksheet.Cell(1, 1).InsertTable(data);
        worksheet.Columns().AdjustToContents();
        workbook.SaveAs(filePath);
        await Task.CompletedTask;
    }
}
