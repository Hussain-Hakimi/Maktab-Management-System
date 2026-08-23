namespace Maktab.Application.Abstractions;

public sealed class AcademicYearDto
{
    public int AcademicYearId { get; set; }
    public string YearName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; }
}

public sealed record SaveAcademicYearDto(
    string YearName,
    DateTime StartDate,
    DateTime EndDate);
