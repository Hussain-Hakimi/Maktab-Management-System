using Maktab.Domain.Enums;

namespace Maktab.Application.Abstractions;

public sealed record SaveAttendanceDto(
    int StudentId,
    DateTime Date,
    AttendanceStatus Status,
    int AcademicYearId);

public sealed class StudentAttendanceDto
{
    public int StudentId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FatherName { get; set; } = string.Empty;
    public string RollNumber { get; set; } = string.Empty;
    public AttendanceStatus Status { get; set; }
}
