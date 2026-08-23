namespace Maktab.Domain.Entities;

public sealed class Fee
{
    public int FeeId { get; set; }
    public int StudentId { get; set; }
    public required string FeeType { get; set; }
    public decimal Amount { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime CreatedDate { get; set; }
    public int AcademicYearId { get; set; }
}
