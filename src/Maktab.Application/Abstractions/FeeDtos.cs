namespace Maktab.Application.Abstractions;

/// <summary>
/// A fee record enriched with payment totals and student info for display.
/// </summary>
public sealed class FeeRecordDto
{
    public int FeeId { get; set; }
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string RollNumber { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public int ClassId { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal AmountDue { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal Outstanding => AmountDue - AmountPaid;
    public DateOnly? DueDate { get; set; }
    public string? AcademicYear { get; set; }
    public bool IsSettled => Outstanding <= 0;
}

/// <summary>
/// A single payment against a fee record, identified by its receipt number.
/// </summary>
public sealed class FeePaymentDto
{
    public int PaymentId { get; set; }
    public int FeeId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string FeeTitle { get; set; } = string.Empty;
    public decimal AmountPaid { get; set; }
    public DateOnly PaymentDate { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
}

/// <summary>
/// Aggregated outstanding fees for one student.
/// </summary>
public sealed class StudentFeeSummaryDto
{
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string RollNumber { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public decimal TotalDue { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal TotalOutstanding => TotalDue - TotalPaid;
    public int OpenFeeCount { get; set; }
}
