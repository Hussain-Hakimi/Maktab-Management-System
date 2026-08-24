using ClosedXML.Excel;
using Maktab.Application.Abstractions;

namespace Maktab.Infrastructure.Reports;

public sealed class ExcelReader : IExcelReader
{
    public IReadOnlyList<string[]> ReadRows(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path is required.", nameof(filePath));
        if (!File.Exists(filePath))
            throw new FileNotFoundException("Excel file not found.", filePath);

        using var workbook = new XLWorkbook(filePath);
        var worksheet = workbook.Worksheets.First();
        var rows = new List<string[]>();

        foreach (var row in worksheet.RowsUsed())
        {
            var cellCount = row.CellsUsed().Count();
            var values = new string[cellCount];
            for (int i = 0; i < cellCount; i++)
            {
                values[i] = row.Cell(i + 1).GetString();
            }
            rows.Add(values);
        }

        return rows;
    }
}
