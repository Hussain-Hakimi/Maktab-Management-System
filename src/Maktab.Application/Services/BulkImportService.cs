using System.Text;
using Maktab.Application.Abstractions;

namespace Maktab.Application.Services;

public sealed class BulkImportService(
    IStudentService studentService,
    IClassSubjectService classSubjectService) : IBulkImportService
{
    public async Task<BulkImportResultDto> ImportStudentsFromCsvAsync(string csvText, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(csvText))
            throw new ArgumentException("CSV content is empty.", nameof(csvText));

        var lines = csvText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length == 0)
            return new BulkImportResultDto();

        var dataLines = lines.Skip(1).ToList();
        var result = new BulkImportResultDto { TotalRows = dataLines.Count };

        var classes = await classSubjectService.GetClassesAsync(cancellationToken);
        var classDict = classes.ToDictionary(c => c.GradeName.Trim(), c => c.ClassId, StringComparer.OrdinalIgnoreCase);

        int lineNumber = 1;
        foreach (var line in dataLines)
        {
            lineNumber++;
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var columns = SplitCsvLine(line);
            if (columns.Length < 5)
            {
                result.Errors.Add($"خط {lineNumber}: تعداد ستون‌ها کمتر از ۵ است.");
                continue;
            }

            var row = new BulkImportStudentRowDto
            {
                FirstName = columns[0].Trim(),
                LastName = columns[1].Trim(),
                FatherName = columns[2].Trim(),
                RollNumber = columns[3].Trim(),
                ClassName = columns[4].Trim()
            };

            if (string.IsNullOrWhiteSpace(row.FirstName) ||
                string.IsNullOrWhiteSpace(row.LastName) ||
                string.IsNullOrWhiteSpace(row.FatherName) ||
                string.IsNullOrWhiteSpace(row.RollNumber) ||
                string.IsNullOrWhiteSpace(row.ClassName))
            {
                result.Errors.Add($"خط {lineNumber}: فیلدهای اجباری خالی هستند.");
                continue;
            }

            if (!classDict.TryGetValue(row.ClassName, out var classId))
            {
                result.Errors.Add($"خط {lineNumber}: صنف «{row.ClassName}» یافت نشد.");
                continue;
            }

            try
            {
                await studentService.RegisterStudentAsync(
                    row.FirstName,
                    row.LastName,
                    row.FatherName,
                    classId,
                    row.RollNumber,
                    cancellationToken);
                result.SuccessCount++;
            }
            catch (Exception ex)
            {
                result.Errors.Add($"خط {lineNumber}: {ex.Message}");
            }
        }

        return result;
    }

    private static string[] SplitCsvLine(string line)
    {
        var result = new List<string>();
        var current = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }
        result.Add(current.ToString());
        return result.ToArray();
    }
}
