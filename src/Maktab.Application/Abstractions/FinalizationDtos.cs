namespace Maktab.Application.Abstractions;

public sealed class ClassFinalizationDto
{
    public int ClassFinalizationId { get; set; }
    public int ClassId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public int AcademicYearId { get; set; }
    public string AcademicYearName { get; set; } = string.Empty;
    public bool IsFinalized { get; set; }
    public int FinalizedByTeacherUserId { get; set; }
    public string FinalizedByTeacherName { get; set; } = string.Empty;
    public DateTime FinalizationDate { get; set; }
}
