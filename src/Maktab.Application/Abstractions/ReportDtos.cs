namespace Maktab.Application.Abstractions;

public sealed class ClassPerformanceReportDto
{
    public string ClassName { get; set; } = string.Empty;
    public string AcademicYear { get; set; } = string.Empty;
    public int TotalStudents { get; set; }
    public decimal OverallAverage { get; set; }
    public int PassCount { get; set; }
    public int FailCount { get; set; }
    public List<SubjectPerformanceDto> SubjectPerformances { get; set; } = [];
}

public sealed class SubjectPerformanceDto
{
    public string SubjectName { get; set; } = string.Empty;
    public decimal AverageScore { get; set; }
    public int PassCount { get; set; }
    public int FailCount { get; set; }
}

public sealed class GradeDistributionDto
{
    public string ClassName { get; set; } = string.Empty;
    public string AcademicYear { get; set; } = string.Empty;
    public int CountA { get; set; }
    public int CountB { get; set; }
    public int CountC { get; set; }
    public int CountD { get; set; }
    public int CountF { get; set; }
}

public sealed class StudentExportRowDto
{
    public int StudentId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FatherName { get; set; } = string.Empty;
    public string RollNumber { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public DateTime RegistrationDate { get; set; }
}

public sealed class MarkExportRowDto
{
    public string StudentName { get; set; } = string.Empty;
    public string RollNumber { get; set; } = string.Empty;
    public string SubjectName { get; set; } = string.Empty;
    public decimal MidtermScore { get; set; }
    public decimal FinalScore { get; set; }
    public decimal TotalScore { get; set; }
    public string Status { get; set; } = string.Empty;
}

public sealed class AttendanceExportRowDto
{
    public string StudentName { get; set; } = string.Empty;
    public string RollNumber { get; set; } = string.Empty;
    public int PresentDays { get; set; }
    public int AbsentDays { get; set; }
    public int IllDays { get; set; }
    public int PermissionDays { get; set; }
    public decimal AbsenceRate { get; set; }
}

public sealed class FeeExportRowDto
{
    public string StudentName { get; set; } = string.Empty;
    public string RollNumber { get; set; } = string.Empty;
    public string FeeType { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal Outstanding { get; set; }
    public string Status { get; set; } = string.Empty;
}
