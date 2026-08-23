using Maktab.Domain.Enums;

namespace Maktab.Application.Abstractions;

public sealed class FeeDto
{
    public int FeeId { get; set; }
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string RollNumber { get; set; } = string.Empty;
    public string FeeType { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime DueDate { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal Outstanding => Amount - TotalPaid;
    public FeeStatus Status { get; set; }
    public int AcademicYearId { get; set; }
}

public sealed class FeePaymentDto
{
    public int PaymentId { get; set; }
    public int FeeId { get; set; }
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string FeeType { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
}

public sealed record SaveFeeDto(
    int StudentId,
    string FeeType,
    decimal Amount,
    DateTime DueDate,
    int AcademicYearId);

public sealed record RecordPaymentDto(
    int FeeId,
    decimal Amount,
    DateTime PaymentDate);
