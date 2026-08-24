using Maktab.Domain.Enums;

namespace Maktab.Domain.Entities;

public sealed class Exam
{
    public int ExamId { get; set; }
    public int SubjectId { get; set; }
    public int ClassId { get; set; }
    public int AcademicYearId { get; set; }
    public ExamType ExamType { get; set; }
    public DateTime ExamDate { get; set; }
    public int CreatedByTeacherUserId { get; set; }
}
