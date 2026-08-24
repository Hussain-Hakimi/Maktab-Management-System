namespace Maktab.Domain.Entities;

public sealed class TeacherSubject
{
    public int TeacherSubjectId { get; set; }
    public int TeacherUserId { get; set; }
    public int ClassId { get; set; }
    public int SubjectId { get; set; }
}
