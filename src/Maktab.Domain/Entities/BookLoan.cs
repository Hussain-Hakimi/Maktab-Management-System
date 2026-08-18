namespace Maktab.Domain.Entities;

public sealed class BookLoan
{
    public int LoanId { get; set; }
    public int BookId { get; set; }
    public int StudentId { get; set; }
    public DateOnly IssueDate { get; set; }
    public DateOnly DueDate { get; set; }
    public DateOnly? ReturnDate { get; set; }

    public bool IsReturned => ReturnDate.HasValue;

    public bool IsOverdue(DateOnly today)
    {
        return !IsReturned && DueDate < today;
    }
}
