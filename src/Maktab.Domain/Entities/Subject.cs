namespace Maktab.Domain.Entities;

public sealed class Subject
{
    public int SubjectId { get; set; }
    public required string SubjectName { get; set; }
    public int ClassId { get; set; }
}