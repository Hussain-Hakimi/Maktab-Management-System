using Maktab.Domain.Enums;

namespace Maktab.Application.Abstractions;

public sealed record SaveExamMarkDto(
    int StudentId,
    int SubjectId,
    decimal MidtermScore,
    decimal FinalScore);

public sealed class StudentExamMarkDto
{
    public int StudentId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FatherName { get; set; } = string.Empty;
    public string RollNumber { get; set; } = string.Empty;
    public int SubjectId { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    public decimal MidtermScore { get; set; }
    public decimal FinalScore { get; set; }
    public decimal TotalScore { get; set; }
    public bool IsPass { get; set; }
}
