namespace Maktab.Domain.Entities;

public sealed class Student
{
    public int StudentId { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string FatherName { get; set; }
    public int ClassId { get; set; }
    public required string RollNumber { get; set; }
    public DateTime RegistrationDate { get; set; } = DateTime.Now;
}
