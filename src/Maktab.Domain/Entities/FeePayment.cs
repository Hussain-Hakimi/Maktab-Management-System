namespace Maktab.Domain.Entities;

public sealed class FeePayment
{
    public int PaymentId { get; set; }
    public int FeeId { get; set; }
    public decimal AmountPaid { get; set; }
    public DateOnly PaymentDate { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
}
