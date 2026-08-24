using ClosedXML.Excel;
using Maktab.Infrastructure.Reports;

namespace Maktab.Tests;

public class ExcelReaderTests
{
    [Fact]
    public void ReadRows_WithValidExcelFile_ReturnsAllRows()
    {
        // Arrange
        var tempFile = Path.Combine(Path.GetTempPath(), "MaktabExcelReaderTest_" + Guid.NewGuid() + ".xlsx");

        using (var workbook = new XLWorkbook())
        {
            var sheet = workbook.Worksheets.Add("Sheet1");
            sheet.Cell(1, 1).Value = "FirstName";
            sheet.Cell(1, 2).Value = "LastName";
            sheet.Cell(2, 1).Value = "Ahmad";
            sheet.Cell(2, 2).Value = "Karimi";
            sheet.Cell(3, 1).Value = "Zahra";
            sheet.Cell(3, 2).Value = "Hussaini";
            workbook.SaveAs(tempFile);
        }

        try
        {
            var reader = new ExcelReader();

            // Act
            var rows = reader.ReadRows(tempFile);

            // Assert
            Assert.Equal(3, rows.Count);
            Assert.Equal("FirstName", rows[0][0]);
            Assert.Equal("Ahmad", rows[1][0]);
            Assert.Equal("Karimi", rows[1][1]);
            Assert.Equal("Zahra", rows[2][0]);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public void ReadRows_WhenFileDoesNotExist_ThrowsFileNotFoundException()
    {
        var reader = new ExcelReader();
        Assert.Throws<FileNotFoundException>(() => reader.ReadRows("nonexistent.xlsx"));
    }
}
