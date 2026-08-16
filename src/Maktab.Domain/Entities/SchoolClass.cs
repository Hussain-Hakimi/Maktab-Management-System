namespace Maktab.Domain.Entities;

public sealed class SchoolClass
{
    public int ClassId { get; set; }
    public required string GradeName { get; set; }
    public int NumberOfSubjects { get; set; }
}