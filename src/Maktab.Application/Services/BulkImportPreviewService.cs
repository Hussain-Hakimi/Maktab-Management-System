using System.Globalization;
using Maktab.Application.Abstractions;
using Maktab.Domain.Enums;
using Maktab.Domain.Rules;

namespace Maktab.Application.Services;

public sealed class BulkImportPreviewService(
    IStudentService studentService,
    IClassSubjectService classSubjectService,
    IExcelReader excelReader) : IBulkImportPreviewService
{
    public Task<BulkImportPreviewResult> PreviewStudentsFromCsvAsync(string csvText, CancellationToken cancellationToken = default)
        => PreviewStudentsAsync(ParseCsvRows(csvText), cancellationToken);

    public Task<BulkImportPreviewResult> PreviewStudentsFromFileAsync(string filePath, CancellationToken cancellationToken = default)
        => PreviewStudentsAsync(ReadRows(filePath), cancellationToken);

    public Task<BulkImportPreviewResult> PreviewMarksFromCsvAsync(string csvText, int classId, int subjectId, int academicYearId, CancellationToken cancellationToken = default)
        => PreviewMarksAsync(ParseCsvRows(csvText), classId, subjectId, academicYearId, cancellationToken);

    public Task<BulkImportPreviewResult> PreviewMarksFromFileAsync(string filePath, int classId, int subjectId, int academicYearId, CancellationToken cancellationToken = default)
        => PreviewMarksAsync(ReadRows(filePath), classId, subjectId, academicYearId, cancellationToken);

    public Task<BulkImportPreviewResult> PreviewAttendanceFromCsvAsync(string csvText, int classId, int academicYearId, CancellationToken cancellationToken = default)
        => PreviewAttendanceAsync(ParseCsvRows(csvText), classId, academicYearId, cancellationToken);

    public Task<BulkImportPreviewResult> PreviewAttendanceFromFileAsync(string filePath, int classId, int academicYearId, CancellationToken cancellationToken = default)
        => PreviewAttendanceAsync(ReadRows(filePath), classId, academicYearId, cancellationToken);

    public async Task<BulkImportPreviewResult> PreviewMultiSubjectMarksFromFileAsync(string filePath, int classId, int academicYearId, CancellationToken cancellationToken = default)
    {
        if (classId <= 0) return Invalid("صنف انتخابی نامعتبر است.");
        if (academicYearId <= 0) return Invalid("سال تعلیمی انتخابی نامعتبر است.");

        var rows = ReadRows(filePath);
        if (rows.Count < 2) return Invalid("فایل باید حداقل یک ردیف معلوماتی داشته باشد.");

        var errors = new List<string>();
        var header = rows[0];
        if (header.Length < 3)
            return Invalid("فارمت فایل نادرست است. حداقل سه ستون لازم است.");

        var subjects = await classSubjectService.GetSubjectsByClassAsync(classId, cancellationToken);
        var subjectByName = subjects.ToDictionary(s => s.SubjectName.Trim(), s => s.SubjectId, StringComparer.OrdinalIgnoreCase);
        var pairs = new List<(int SubjectId, int Midterm, int Final)>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var col = 1; col < header.Length; col++)
        {
            var name = header[col].Trim();
            if (!name.EndsWith("_Midterm", StringComparison.OrdinalIgnoreCase)) continue;
            var subjectName = name[..^"_Midterm".Length].Trim();
            var finalName = $"{subjectName}_Final";
            var finalCol = -1;
            for (var i = col + 1; i < header.Length; i++)
                if (string.Equals(header[i].Trim(), finalName, StringComparison.OrdinalIgnoreCase)) { finalCol = i; break; }

            if (finalCol < 0) { errors.Add($"ستون «{name}» ستون متناظر «{finalName}» را ندارد."); continue; }
            if (!subjectByName.TryGetValue(subjectName, out var subjectId)) { errors.Add($"مضمون «{subjectName}» در صنف انتخابی یافت نشد."); continue; }
            if (seen.Add(subjectName)) pairs.Add((subjectId, col, finalCol));
        }

        if (pairs.Count == 0) { errors.Add("هیچ جفت ستون مضمون معتبر یافت نشد."); return new BulkImportPreviewResult(Math.Max(0, rows.Count - 1), 0, errors); }

        var students = await studentService.GetStudentsByClassAsync(classId, cancellationToken);
        var studentByRoll = students.ToDictionary(s => s.RollNumber.Trim(), s => s.StudentId, StringComparer.OrdinalIgnoreCase);
        var valid = 0;
        for (var rowIndex = 1; rowIndex < rows.Count; rowIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var row = rows[rowIndex];
            if (row.Length == 0 || string.IsNullOrWhiteSpace(row[0])) { errors.Add($"خط {rowIndex + 1}: شماره اساس خالی است."); continue; }
            var roll = row[0].Trim();
            if (!studentByRoll.ContainsKey(roll)) { errors.Add($"خط {rowIndex + 1}: شاگرد با شماره اساس «{roll}» یافت نشد."); continue; }
            var rowValid = true;
            foreach (var pair in pairs)
            {
                var midRaw = pair.Midterm < row.Length ? row[pair.Midterm].Trim() : "";
                var finalRaw = pair.Final < row.Length ? row[pair.Final].Trim() : "";
                if (string.IsNullOrWhiteSpace(midRaw) && string.IsNullOrWhiteSpace(finalRaw)) continue;
                if (!TryScore(midRaw, GradingPolicy.MidtermMax, out _)) { errors.Add($"خط {rowIndex + 1}: نمره چهارونیم‌ماهه «{header[pair.Midterm]}» نامعتبر است."); rowValid = false; break; }
                if (!TryScore(finalRaw, GradingPolicy.FinalMax, out _)) { errors.Add($"خط {rowIndex + 1}: نمره سالانه «{header[pair.Final]}» نامعتبر است."); rowValid = false; break; }
            }
            if (rowValid) valid++;
        }
        return new BulkImportPreviewResult(rows.Count - 1, valid, errors);
    }

    private async Task<BulkImportPreviewResult> PreviewStudentsAsync(IReadOnlyList<string[]> rows, CancellationToken cancellationToken)
    {
        if (rows.Count < 2) return Invalid("فایل باید حداقل یک ردیف معلوماتی داشته باشد.");
        var errors = new List<string>();
        var classes = await classSubjectService.GetClassesAsync(cancellationToken);
        var classNames = classes.Select(c => c.GradeName.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var seenRolls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var valid = 0;
        for (var i = 1; i < rows.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var row = rows[i];
            if (row.Length < 5) { errors.Add($"خط {i + 1}: تعداد ستون‌ها کمتر از ۵ است."); continue; }
            var values = row.Take(5).Select(x => x.Trim()).ToArray();
            if (values.Any(string.IsNullOrWhiteSpace)) { errors.Add($"خط {i + 1}: فیلدهای اجباری خالی هستند."); continue; }
            if (!classNames.Contains(values[4])) { errors.Add($"خط {i + 1}: صنف «{values[4]}» یافت نشد."); continue; }
            if (!seenRolls.Add(values[3])) { errors.Add($"خط {i + 1}: شماره اساس «{values[3]}» در همین فایل تکراری است."); continue; }
            valid++;
        }
        return new BulkImportPreviewResult(rows.Count - 1, valid, errors);
    }

    private async Task<BulkImportPreviewResult> PreviewMarksAsync(IReadOnlyList<string[]> rows, int classId, int subjectId, int academicYearId, CancellationToken cancellationToken)
    {
        if (classId <= 0 || subjectId <= 0 || academicYearId <= 0) return Invalid("صنف، مضمون یا سال تعلیمی نامعتبر است.");
        if (rows.Count < 2) return Invalid("فایل باید حداقل یک ردیف معلوماتی داشته باشد.");
        var errors = new List<string>();
        var subjects = await classSubjectService.GetSubjectsByClassAsync(classId, cancellationToken);
        if (!subjects.Any(s => s.SubjectId == subjectId)) return Invalid("مضمون انتخاب‌شده مربوط به صنف نیست.");
        var students = await studentService.GetStudentsByClassAsync(classId, cancellationToken);
        var rolls = students.Select(s => s.RollNumber.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var valid = 0;
        for (var i = 1; i < rows.Count; i++)
        {
            var row = rows[i];
            if (row.Length < 3) { errors.Add($"خط {i + 1}: تعداد ستون‌ها کمتر از ۳ است."); continue; }
            var roll = row[0].Trim();
            if (string.IsNullOrWhiteSpace(roll) || !rolls.Contains(roll)) { errors.Add($"خط {i + 1}: شماره اساس نامعتبر است."); continue; }
            if (!seen.Add(roll)) { errors.Add($"خط {i + 1}: شماره اساس «{roll}» تکراری است."); continue; }
            if (!TryScore(row[1], GradingPolicy.MidtermMax, out _) || !TryScore(row[2], GradingPolicy.FinalMax, out _)) { errors.Add($"خط {i + 1}: نمره نامعتبر است."); continue; }
            valid++;
        }
        return new BulkImportPreviewResult(rows.Count - 1, valid, errors);
    }

    private async Task<BulkImportPreviewResult> PreviewAttendanceAsync(IReadOnlyList<string[]> rows, int classId, int academicYearId, CancellationToken cancellationToken)
    {
        if (classId <= 0 || academicYearId <= 0) return Invalid("صنف یا سال تعلیمی نامعتبر است.");
        if (rows.Count < 2) return Invalid("فایل باید حداقل یک ردیف معلوماتی داشته باشد.");
        var errors = new List<string>();
        var students = await studentService.GetStudentsByClassAsync(classId, cancellationToken);
        var rolls = students.Select(s => s.RollNumber.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var valid = 0;
        for (var i = 1; i < rows.Count; i++)
        {
            var row = rows[i];
            if (row.Length < 3) { errors.Add($"خط {i + 1}: تعداد ستون‌ها کمتر از ۳ است."); continue; }
            var roll = row[0].Trim();
            if (string.IsNullOrWhiteSpace(roll) || !rolls.Contains(roll)) { errors.Add($"خط {i + 1}: شماره اساس نامعتبر است."); continue; }
            if (!DateTime.TryParse(row[1].Trim(), CultureInfo.InvariantCulture, DateTimeStyles.None, out _)) { errors.Add($"خط {i + 1}: تاریخ نامعتبر است."); continue; }
            if (!Enum.TryParse<AttendanceStatus>(row[2].Trim(), true, out _)) { errors.Add($"خط {i + 1}: وضعیت حاضری نامعتبر است."); continue; }
            valid++;
        }
        return new BulkImportPreviewResult(rows.Count - 1, valid, errors);
    }

    private IReadOnlyList<string[]> ReadRows(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("مسیر فایل خالی است.", nameof(filePath));
        return Path.GetExtension(filePath).Equals(".csv", StringComparison.OrdinalIgnoreCase)
            ? ParseCsvRows(File.ReadAllText(filePath))
            : excelReader.ReadRows(filePath);
    }

    private static BulkImportPreviewResult Invalid(string message)
        => new(0, 0, new[] { message });

    private static bool TryScore(string raw, decimal max, out decimal score)
        => decimal.TryParse(raw?.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out score) && score >= 0m && score <= max;

    private static List<string[]> ParseCsvRows(string csvText)
    {
        if (string.IsNullOrWhiteSpace(csvText)) return [];
        var rows = new List<string[]>();
        var row = new List<string>();
        var field = new System.Text.StringBuilder();
        var quoted = false;
        for (var i = 0; i < csvText.Length; i++)
        {
            var c = csvText[i];
            if (c == '"')
            {
                if (quoted && i + 1 < csvText.Length && csvText[i + 1] == '"') { field.Append('"'); i++; }
                else quoted = !quoted;
            }
            else if (c == ',' && !quoted) { row.Add(field.ToString()); field.Clear(); }
            else if ((c == '\n' || c == '\r') && !quoted)
            {
                if (c == '\r' && i + 1 < csvText.Length && csvText[i + 1] == '\n') i++;
                row.Add(field.ToString()); field.Clear();
                rows.Add(row.ToArray()); row.Clear();
            }
            else field.Append(c);
        }
        if (field.Length > 0 || row.Count > 0) { row.Add(field.ToString()); rows.Add(row.ToArray()); }
        return rows;
    }
}
