namespace Maktab.Domain.Entities;

public sealed class ExamMark
{
    public int StudentId { get; set; }
    public int SubjectId { get; set; }
    public decimal MidtermScore { get; set; }
    public decimal FinalScore { get; set; }
    public int AcademicYearId { get; set; }
}
