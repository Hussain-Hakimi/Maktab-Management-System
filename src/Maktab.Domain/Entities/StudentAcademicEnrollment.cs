namespace Maktab.Domain.Entities;

public sealed class StudentAcademicEnrollment
{
    public int EnrollmentId { get; set; }
    public int StudentId { get; set; }
    public int AcademicYearId { get; set; }
    public int ClassId { get; set; }
    public required string RollNumber { get; set; }
    public DateTime EnrollmentDate { get; set; } = DateTime.Now;
    public required string Status { get; set; }
}
