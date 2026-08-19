namespace Maktab.Domain.Entities;

public sealed class FeePayment
{
    public int PaymentId { get; set; }
    public int FeeId { get; set; }
    public int StudentId { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
    public required string ReceiptNumber { get; set; }
}
