namespace Maktab.Domain.Entities;

public sealed class TextbookIssue
{
    public int IssueId { get; set; }
    public int TextbookId { get; set; }
    public int StudentId { get; set; }
    public DateOnly IssueDate { get; set; }
    public DateOnly? ReturnDate { get; set; }

    public bool IsReturned => ReturnDate is not null;
}
