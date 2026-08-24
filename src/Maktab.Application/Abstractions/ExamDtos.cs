using Maktab.Domain.Enums;

namespace Maktab.Application.Abstractions;

public sealed class ExamDto
{
    public int ExamId { get; set; }
    public int SubjectId { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    public int ClassId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public int AcademicYearId { get; set; }
    public string AcademicYearName { get; set; } = string.Empty;
    public ExamType ExamType { get; set; }
    public DateTime ExamDate { get; set; }
    public int CreatedByTeacherUserId { get; set; }
    public string CreatedByTeacherName { get; set; } = string.Empty;
}

public sealed record SaveExamDto(
    int SubjectId,
    int ClassId,
    int AcademicYearId,
    ExamType ExamType,
    DateTime ExamDate,
    int CreatedByTeacherUserId);
