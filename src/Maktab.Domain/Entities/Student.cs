namespace Maktab.Domain.Entities;

public sealed class Student
{
    public int StudentId { get; set; }
    /// <summary>
    /// Stable, human-facing admission identifier. It is assigned once and must not change when the student changes class or academic year.
    /// </summary>
    public string? AdmissionNumber { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string FatherName { get; set; }
    public int ClassId { get; set; }
    public required string RollNumber { get; set; }
    public DateTime RegistrationDate { get; set; } = DateTime.Now;
}
