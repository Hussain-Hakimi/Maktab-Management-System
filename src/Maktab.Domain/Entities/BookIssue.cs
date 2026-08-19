using Maktab.Domain.Enums;

namespace Maktab.Domain.Entities;

public sealed class BookIssue
{
    public int IssueId { get; set; }
    public int BookId { get; set; }
    public int StudentId { get; set; }
    public DateTime IssueDate { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime? ReturnDate { get; set; }
    public BookIssueStatus Status { get; set; }
}
