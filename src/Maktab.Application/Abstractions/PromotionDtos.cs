namespace Maktab.Application.Abstractions;

public sealed class PromotionResultDto
{
    public int TotalStudents { get; set; }
    public int PromotedCount { get; set; }
    public int ConditionalCount { get; set; }
    public int RepeatCount { get; set; }
    public List<string> Errors { get; set; } = [];
}

public sealed class PromotionHistoryDto
{
    public int PromotionId { get; set; }
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string RollNumber { get; set; } = string.Empty;
    public string FromClassName { get; set; } = string.Empty;
    public string? ToClassName { get; set; }
    public string AcademicYearName { get; set; } = string.Empty;
    public string Result { get; set; } = string.Empty;
    public DateTime PromotionDate { get; set; }
}
