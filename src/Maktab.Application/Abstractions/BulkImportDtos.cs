namespace Maktab.Application.Abstractions;

public sealed class BulkImportResultDto
{
    public int TotalRows { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount => TotalRows - SuccessCount;
    public List<string> Errors { get; set; } = [];
}

public sealed class BulkImportStudentRowDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FatherName { get; set; } = string.Empty;
    public string RollNumber { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
}

public sealed class MarkImportRowDto
{
    public string RollNumber { get; set; } = string.Empty;
    public string SubjectName { get; set; } = string.Empty;
    public decimal MidtermScore { get; set; }
    public decimal FinalScore { get; set; }
}

public sealed class AttendanceImportRowDto
{
    public string RollNumber { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string Status { get; set; } = string.Empty;
}
