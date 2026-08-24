using System.Globalization;
using System.Text;
using Maktab.Application.Abstractions;
using Maktab.Domain.Entities;
using Maktab.Domain.Enums;
using Maktab.Domain.Rules;

namespace Maktab.Application.Services;

public sealed class BulkImportService(
    IStudentService studentService,
    IClassSubjectService classSubjectService,
    IExamMarkService examMarkService,
    IAttendanceService attendanceService,
    IExcelReader excelReader) : IBulkImportService
{
    // ---------- CSV (text) entry points ----------

    public Task<BulkImportResultDto> ImportStudentsFromCsvAsync(string csvText, CancellationToken cancellationToken = default)
    {
        var rows = ParseCsvRows(csvText);
        return ImportStudentsFromRowsAsync(rows, cancellationToken);
    }

    public Task<BulkImportResultDto> ImportMarksFromCsvAsync(
        string csvText,
        int classId,
        int subjectId,
        int academicYearId,
        CancellationToken cancellationToken = default)
    {
        var rows = ParseCsvRows(csvText);
        return ImportMarksFromRowsAsync(rows, classId, subjectId, academicYearId, cancellationToken);
    }

    public Task<BulkImportResultDto> ImportAttendanceFromCsvAsync(
        string csvText,
        int classId,
        int academicYearId,
        CancellationToken cancellationToken = default)
    {
        var rows = ParseCsvRows(csvText);
        return ImportAttendanceFromRowsAsync(rows, classId, academicYearId, cancellationToken);
    }

    // ---------- File (auto-detect) entry points ----------

    public async Task<BulkImportResultDto> ImportStudentsFromFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var rows = ReadRowsFromFile(filePath);
        return await ImportStudentsFromRowsAsync(rows, cancellationToken);
    }

    public async Task<BulkImportResultDto> ImportMarksFromFileAsync(
        string filePath,
        int classId,
        int subjectId,
        int academicYearId,
        CancellationToken cancellationToken = default)
    {
        var rows = ReadRowsFromFile(filePath);
        return await ImportMarksFromRowsAsync(rows, classId, subjectId, academicYearId, cancellationToken);
    }

    public async Task<BulkImportResultDto> ImportAttendanceFromFileAsync(
        string filePath,
        int classId,
        int academicYearId,
        CancellationToken cancellationToken = default)
    {
        var rows = ReadRowsFromFile(filePath);
        return await ImportAttendanceFromRowsAsync(rows, classId, academicYearId, cancellationToken);
    }

    // ---------- Core row processing ----------

    private async Task<BulkImportResultDto> ImportStudentsFromRowsAsync(
        IEnumerable<string[]> rows,
        CancellationToken cancellationToken)
    {
        var result = new BulkImportResultDto();
        var classes = await classSubjectService.GetClassesAsync(cancellationToken);
        var classDict = classes.ToDictionary(c => c.GradeName.Trim(), c => c.ClassId, StringComparer.OrdinalIgnoreCase);

        int lineNumber = 0;
        foreach (var columns in rows)
        {
            lineNumber++;
            if (lineNumber == 1) continue; // skip header
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
                    row.FirstName, row.LastName, row.FatherName, classId, row.RollNumber, cancellationToken);
                result.SuccessCount++;
            }
            catch (Exception ex)
            {
                result.Errors.Add($"خط {lineNumber}: {ex.Message}");
            }
        }

        result.TotalRows = rows.Count() - 1;
        if (result.TotalRows < 0) result.TotalRows = 0;
        return result;
    }

    private async Task<BulkImportResultDto> ImportMarksFromRowsAsync(
        IEnumerable<string[]> rows,
        int classId,
        int subjectId,
        int academicYearId,
        CancellationToken cancellationToken)
    {
        if (classId <= 0) throw new ArgumentOutOfRangeException(nameof(classId));
        if (subjectId <= 0) throw new ArgumentOutOfRangeException(nameof(subjectId));
        if (academicYearId <= 0) throw new ArgumentOutOfRangeException(nameof(academicYearId));

        var result = new BulkImportResultDto();
        var students = await studentService.GetStudentsByClassAsync(classId, cancellationToken);
        var studentByRoll = students.ToDictionary(s => s.RollNumber.Trim(), s => s.StudentId, StringComparer.OrdinalIgnoreCase);
        var validMarks = new List<SaveExamMarkDto>();

        int lineNumber = 0;
        foreach (var columns in rows)
        {
            lineNumber++;
            if (lineNumber == 1) continue; // skip header

            if (columns.Length < 3)
            {
                result.Errors.Add($"خط {lineNumber}: تعداد ستون‌ها کمتر از ۳ است.");
                continue;
            }

            var rollNumber = columns[0].Trim();
            if (!decimal.TryParse(columns[1], NumberStyles.Number, CultureInfo.InvariantCulture, out var midterm) ||
                !decimal.TryParse(columns[2], NumberStyles.Number, CultureInfo.InvariantCulture, out var final))
            {
                result.Errors.Add($"خط {lineNumber}: نمره نامعتبر است.");
                continue;
            }

            if (midterm < 0m || midterm > GradingPolicy.MidtermMax)
            {
                result.Errors.Add($"خط {lineNumber}: نمره چهارونیم‌ماهه باید بین ۰ و {GradingPolicy.MidtermMax} باشد.");
                continue;
            }
            if (final < 0m || final > GradingPolicy.FinalMax)
            {
                result.Errors.Add($"خط {lineNumber}: نمره سالانه باید بین ۰ و {GradingPolicy.FinalMax} باشد.");
                continue;
            }
            if (!studentByRoll.TryGetValue(rollNumber, out var studentId))
            {
                result.Errors.Add($"خط {lineNumber}: شاگرد با شماره اساس «{rollNumber}» یافت نشد.");
                continue;
            }

            validMarks.Add(new SaveExamMarkDto(
                StudentId: studentId,
                SubjectId: subjectId,
                MidtermScore: midterm,
                FinalScore: final,
                AcademicYearId: academicYearId));
            result.SuccessCount++;
        }

        if (validMarks.Count > 0)
        {
            try
            {
                await examMarkService.SaveMarksBatchAsync(validMarks, cancellationToken);
            }
            catch (Exception ex)
            {
                result.Errors.Add($"خطا در ذخیره نمرات: {ex.Message}");
                result.SuccessCount -= validMarks.Count;
            }
        }

        result.TotalRows = rows.Count() - 1;
        if (result.TotalRows < 0) result.TotalRows = 0;
        return result;
    }

    private async Task<BulkImportResultDto> ImportAttendanceFromRowsAsync(
        IEnumerable<string[]> rows,
        int classId,
        int academicYearId,
        CancellationToken cancellationToken)
    {
        if (classId <= 0) throw new ArgumentOutOfRangeException(nameof(classId));
        if (academicYearId <= 0) throw new ArgumentOutOfRangeException(nameof(academicYearId));

        var result = new BulkImportResultDto();
        var students = await studentService.GetStudentsByClassAsync(classId, cancellationToken);
        var studentByRoll = students.ToDictionary(s => s.RollNumber.Trim(), s => s.StudentId, StringComparer.OrdinalIgnoreCase);
        var validAttendance = new List<SaveAttendanceDto>();

        int lineNumber = 0;
        foreach (var columns in rows)
        {
            lineNumber++;
            if (lineNumber == 1) continue; // skip header

            if (columns.Length < 3)
            {
                result.Errors.Add($"خط {lineNumber}: تعداد ستون‌ها کمتر از ۳ است.");
                continue;
            }

            var rollNumber = columns[0].Trim();
            if (!DateTime.TryParse(columns[1], out var date))
            {
                result.Errors.Add($"خط {lineNumber}: تاریخ نامعتبر است.");
                continue;
            }

            var status = ParseAttendanceStatus(columns[2].Trim());
            if (status == null)
            {
                result.Errors.Add($"خط {lineNumber}: وضعیت حاضری نامعتبر است.");
                continue;
            }

            if (!studentByRoll.TryGetValue(rollNumber, out var studentId))
            {
                result.Errors.Add($"خط {lineNumber}: شاگرد با شماره اساس «{rollNumber}» یافت نشد.");
                continue;
            }

            validAttendance.Add(new SaveAttendanceDto(
                StudentId: studentId,
                Date: date,
                Status: status.Value,
                AcademicYearId: academicYearId));
            result.SuccessCount++;
        }

        if (validAttendance.Count > 0)
        {
            try
            {
                await attendanceService.SaveAttendanceBatchAsync(validAttendance, cancellationToken);
            }
            catch (Exception ex)
            {
                result.Errors.Add($"خطا در ذخیره حاضری: {ex.Message}");
                result.SuccessCount -= validAttendance.Count;
            }
        }

        result.TotalRows = rows.Count() - 1;
        if (result.TotalRows < 0) result.TotalRows = 0;
        return result;
    }

    // ---------- Helpers ----------

    private IReadOnlyList<string[]> ReadRowsFromFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path is required.", nameof(filePath));

        var extension = Path.GetExtension(filePath).ToLowerInvariant();

        if (extension == ".xlsx")
            return excelReader.ReadRows(filePath);

        if (extension == ".csv")
        {
            var text = File.ReadAllText(filePath);
            return ParseCsvRows(text);
        }

        throw new NotSupportedException($"فرمت فایل «{extension}» پشتیبانی نمی‌شود.");
    }

    private static IReadOnlyList<string[]> ParseCsvRows(string csvText)
    {
        if (string.IsNullOrWhiteSpace(csvText))
            return new List<string[]>();

        var lines = csvText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        var rows = new List<string[]>();
        foreach (var line in lines)
        {
            rows.Add(SplitCsvLine(line));
        }
        return rows;
    }

    private static AttendanceStatus? ParseAttendanceStatus(string status)
    {
        return status.ToLowerInvariant() switch
        {
            "present" or "حاضر" => AttendanceStatus.Present,
            "absent" or "غایب" or "غائب" => AttendanceStatus.Absent,
            "ill" or "مریض" => AttendanceStatus.Ill,
            "permission" or "اجازه" => AttendanceStatus.Permission,
            _ => null
        };
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
