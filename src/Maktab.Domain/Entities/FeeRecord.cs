namespace Maktab.Domain.Entities;

public sealed class FeeRecord
{
    public int FeeId { get; set; }
    public int StudentId { get; set; }
    public required string Title { get; set; }
    public decimal AmountDue { get; set; }
    public DateOnly? DueDate { get; set; }
    public string? AcademicYear { get; set; }
    public DateTime CreatedDate { get; set; }
}
