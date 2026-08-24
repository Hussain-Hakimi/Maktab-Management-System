namespace Maktab.Domain.Entities;

public sealed class ClassGuardian
{
    public int ClassGuardianId { get; set; }
    public int TeacherUserId { get; set; }
    public int ClassId { get; set; }
}
