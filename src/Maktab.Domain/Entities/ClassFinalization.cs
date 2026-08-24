namespace Maktab.Domain.Entities;

public sealed class ClassFinalization
{
    public int ClassFinalizationId { get; set; }
    public int ClassId { get; set; }
    public int AcademicYearId { get; set; }
    public bool IsFinalized { get; set; }
    public int FinalizedByTeacherUserId { get; set; }
    public DateTime FinalizationDate { get; set; }
}
