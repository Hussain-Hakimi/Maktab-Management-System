namespace Maktab.Domain.Entities;

public sealed class AcademicYear
{
    public int AcademicYearId { get; set; }
    public required string YearName { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; }
}
