namespace Maktab.Application.Abstractions;

public sealed class BookLoanDto
{
    public int LoanId { get; set; }
    public int BookId { get; set; }
    public string BookTitle { get; set; } = string.Empty;
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string RollNumber { get; set; } = string.Empty;
    public DateOnly IssueDate { get; set; }
    public DateOnly DueDate { get; set; }
    public DateOnly? ReturnDate { get; set; }
    public bool IsReturned => ReturnDate.HasValue;
    public bool IsOverdue { get; set; }
}
