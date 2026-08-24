namespace Maktab.Application.Abstractions;

public interface IExcelReader
{
    IReadOnlyList<string[]> ReadRows(string filePath);
}
