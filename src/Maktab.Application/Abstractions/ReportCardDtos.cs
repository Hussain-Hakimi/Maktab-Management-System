using Maktab.Domain.Enums;

namespace Maktab.Application.Abstractions;

public sealed class SubjectMarkReportDto
{
    public string SubjectName { get; set; } = string.Empty;
    public decimal MidtermScore { get; set; }
    public decimal FinalScore { get; set; }
    public decimal TotalScore { get; set; }
    public bool IsPass { get; set; }
}

public sealed class StudentReportCardDto
{
    public int StudentId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FatherName { get; set; } = string.Empty;
    public string RollNumber { get; set; } = string.Empty;
    public int ClassId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public string AcademicYear { get; set; } = string.Empty;
    public string IssueDate { get; set; } = string.Empty;

    public IReadOnlyList<SubjectMarkReportDto> SubjectMarks { get; set; } = [];

    public decimal TotalObtainedScore { get; set; }
    public decimal TotalMaxScore { get; set; }
    public decimal AveragePercentage { get; set; }
    public LetterGrade OverallGrade { get; set; }
    public int PassedSubjectsCount { get; set; }
    public int FailedSubjectsCount { get; set; }
    public int AbsenceDays { get; set; }
    public PromotionOutcome PromotionOutcome { get; set; }
    public string PromotionStatusText { get; set; } = string.Empty;
    public string? FailureReason { get; set; }

    // New fields for customizable headers
    public string GovernmentTitle { get; set; } = "امارت اسلامی افغانستان";
    public string ProvincialEducationHeader { get; set; } = string.Empty;
    public string DistrictEducationHeader { get; set; } = string.Empty;
    public string SchoolLogoPath { get; set; } = string.Empty;

    // New field for report type
    public ReportCardType ReportType { get; set; } = ReportCardType.Annual;
}
