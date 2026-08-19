using Maktab.Domain.Enums;

namespace Maktab.Domain.Entities;

public sealed class TextbookIssue
{
    public int IssueId { get; set; }
    public int TextbookId { get; set; }
    public int StudentId { get; set; }
    public DateTime IssueDate { get; set; }
    public DateTime? ReturnDate { get; set; }
    public TextbookIssueStatus Status { get; set; }
}
