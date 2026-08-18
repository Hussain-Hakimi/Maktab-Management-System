using ClosedXML.Excel;
using Maktab.Application.Abstractions;
using Maktab.Domain.Enums;

namespace Maktab.Infrastructure.Excel;

/// <summary>
/// Offline Excel integration for attendance: generates pre-filled class templates
/// (every day defaults to "حاضر" so only exceptions are edited) and imports the
/// filled files back into the database.
/// </summary>
public sealed class AttendanceExcelService(
    IStudentRepository studentRepository,
    IClassSubjectRepository classSubjectRepository) : IAttendanceExcelService
{
    private const string PresentWord = "حاضر";

    public async Task GenerateClassTemplateAsync(
        int classId,
        DateOnly startDate,
        int numberOfDays,
        string outputFilePath,
        CancellationToken cancellationToken = default)
    {
        if (classId <= 0) throw new ArgumentOutOfRangeException(nameof(classId));
        if (numberOfDays < 1 || numberOfDays > 31)
        {
            throw new ArgumentOutOfRangeException(nameof(numberOfDays), "Number of days must be between 1 and 31.");
        }
        if (string.IsNullOrWhiteSpace(outputFilePath)) throw new ArgumentException("Output path is required.", nameof(outputFilePath));

        var classes = await classSubjectRepository.GetClassesAsync(cancellationToken);
        var schoolClass = classes.FirstOrDefault(c => c.ClassId == classId)
            ?? throw new InvalidOperationException($"صنف با آیدی {classId} یافت نشد.");

        var students = await studentRepository.GetStudentsByClassAsync(classId, cancellationToken);

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Attendance");
        sheet.RightToLeft = true;

        // Header block — ClassID in a fixed cell (D1) so import can identify the class
        sheet.Cell(1, 1).Value = "کلاس / Class:";
        sheet.Cell(1, 2).Value = schoolClass.GradeName;
        sheet.Cell(1, 3).Value = "ClassID:";
        sheet.Cell(1, 4).Value = classId;

        sheet.Cell(2, 1).Value =
            "راهنما: هر خانۀ حاضری را تغییر ندهید مگر برای استثناها. کلمات قابل قبول: " +
            "حاضر (Present) / غیرحاضر (Absent) / مریض (Ill) / اجازه (Permission). " +
            "خانه‌های خالی ثبت نمی‌شوند.";
        sheet.Range(2, 1, 2, 4 + numberOfDays).Merge();
        sheet.Cell(2, 1).Style.Alignment.WrapText = true;

        // Table header row
        const int headerRow = 4;
        sheet.Cell(headerRow, 1).Value = "StudentID";
        sheet.Cell(headerRow, 2).Value = "شماره اساس";
        sheet.Cell(headerRow, 3).Value = "نام";
        sheet.Cell(headerRow, 4).Value = "تخلص";

        for (var d = 0; d < numberOfDays; d++)
        {
            var date = startDate.AddDays(d);
            sheet.Cell(headerRow, 5 + d).Value = date.ToString("yyyy-MM-dd");
        }

        var headerRange = sheet.Range(headerRow, 1, headerRow, 4 + numberOfDays);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#1E3A8A");
        headerRange.Style.Font.FontColor = XLColor.White;

        // One row per student, every day pre-filled with Present
        var rowIndex = headerRow + 1;
        foreach (var student in students.OrderBy(s => s.RollNumber))
        {
            sheet.Cell(rowIndex, 1).Value = student.StudentId;
            sheet.Cell(rowIndex, 2).Value = student.RollNumber;
            sheet.Cell(rowIndex, 3).Value = student.FirstName;
            sheet.Cell(rowIndex, 4).Value = student.LastName;

            for (var d = 0; d < numberOfDays; d++)
            {
                sheet.Cell(rowIndex, 5 + d).Value = PresentWord;
            }

            rowIndex++;
        }

        sheet.Column(1).Style.Protection.Locked = true;
        sheet.Columns(1, 4).AdjustToContents();
        for (var d = 0; d < numberOfDays; d++)
        {
            sheet.Column(5 + d).Width = 12;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputFilePath))!);
        workbook.SaveAs(outputFilePath);
    }

    public Task<AttendanceImportResultDto> ImportTemplateAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("File path is required.", nameof(filePath));
        if (!File.Exists(filePath)) throw new FileNotFoundException("فایل اکسل یافت نشد.", filePath);

        var result = new AttendanceImportResultDto();

        using var workbook = new XLWorkbook(filePath);
        var sheet = workbook.Worksheets.FirstOrDefault();
        if (sheet is null)
        {
            result.Errors.Add("فایل اکسل هیچ صفحه‌ای ندارد.");
            return Task.FromResult(result);
        }

        // Class identity from the fixed metadata cells
        if (!sheet.Cell(1, 4).TryGetValue<int>(out var classId) || classId <= 0)
        {
            result.Errors.Add("کد صنف (ClassID) در خانۀ D1 یافت نشد — از همان تمپلتی استفاده کنید که سیستم صادر کرده است.");
            return Task.FromResult(result);
        }

        result.ClassId = classId;
        result.ClassName = sheet.Cell(1, 2).GetString();

        const int headerRow = 4;
        var lastColumn = sheet.Row(headerRow).LastCellUsed()?.Address.ColumnNumber ?? 4;
        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? headerRow;

        // Date columns start at column 5
        var dateColumns = new List<(int Column, DateOnly Date)>();
        for (var col = 5; col <= lastColumn; col++)
        {
            var headerText = sheet.Cell(headerRow, col).GetString().Trim();
            if (DateOnly.TryParseExact(headerText, "yyyy-MM-dd", out var date))
            {
                dateColumns.Add((col, date));
            }
        }

        if (dateColumns.Count == 0)
        {
            result.Errors.Add("هیچ ستون تاریخی (yyyy-MM-dd) در ردیف عنوان یافت نشد.");
            return Task.FromResult(result);
        }

        for (var row = headerRow + 1; row <= lastRow; row++)
        {
            if (!sheet.Cell(row, 1).TryGetValue<int>(out var studentId) || studentId <= 0)
            {
                // Skip fully empty trailing rows silently, flag broken ones
                if (!sheet.Cell(row, 2).IsEmpty() || !sheet.Cell(row, 3).IsEmpty())
                {
                    result.Errors.Add($"ردیف {row}: StudentID نامعتبر است.");
                }
                continue;
            }

            foreach (var (column, date) in dateColumns)
            {
                var raw = sheet.Cell(row, column).GetString().Trim();
                if (string.IsNullOrWhiteSpace(raw))
                {
                    continue; // empty cell = no record for that day
                }

                if (TryParseStatus(raw, out var status))
                {
                    result.Rows.Add(new SaveAttendanceDto(studentId, date, status, Notes: null));
                }
                else
                {
                    result.Errors.Add($"ردیف {row}، ستون {date:yyyy-MM-dd}: وضعیت «{raw}» قابل تشخیص نیست.");
                }
            }
        }

        return Task.FromResult(result);
    }

    /// <summary>
    /// Accepts Dari words, English words and single-letter codes for each status.
    /// </summary>
    private static bool TryParseStatus(string raw, out AttendanceStatus status)
    {
        switch (raw.Trim())
        {
            case "حاضر":
            case "حاضره":
            case "present":
            case "Present":
            case "P":
            case "p":
                status = AttendanceStatus.Present;
                return true;
            case "غیرحاضر":
            case "غائب":
            case "absent":
            case "Absent":
            case "A":
            case "a":
                status = AttendanceStatus.Absent;
                return true;
            case "مریض":
            case "مريض":
            case "ill":
            case "Ill":
            case "sick":
            case "Sick":
            case "I":
            case "i":
                status = AttendanceStatus.Ill;
                return true;
            case "اجازه":
            case "رخصت":
            case "permission":
            case "Permission":
            case "leave":
            case "Leave":
            case "L":
            case "l":
                status = AttendanceStatus.Permission;
                return true;
            default:
                status = default;
                return false;
        }
    }
}
