namespace Maktab.Application.Abstractions;

public sealed class FeeRecordDto
{
    public int FeeId { get; set; }
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string RollNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public decimal AmountDue { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal Remaining => AmountDue - AmountPaid;
    public bool IsFullyPaid => Remaining <= 0m;
    public DateOnly DueDate { get; set; }
    public string AcademicYear { get; set; } = string.Empty;
}

public sealed class FeePaymentDto
{
    public int PaymentId { get; set; }
    public int FeeId { get; set; }
    public decimal AmountPaid { get; set; }
    public DateOnly PaymentDate { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
}
