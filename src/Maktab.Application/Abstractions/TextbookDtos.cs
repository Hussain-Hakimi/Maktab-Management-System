namespace Maktab.Application.Abstractions;

/// <summary>
/// A textbook issue enriched with textbook title and student name for display.
/// </summary>
public sealed class TextbookIssueDto
{
    public int IssueId { get; set; }
    public int TextbookId { get; set; }
    public string TextbookTitle { get; set; } = string.Empty;
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string RollNumber { get; set; } = string.Empty;
    public DateOnly IssueDate { get; set; }
    public DateOnly? ReturnDate { get; set; }
    public bool IsReturned { get; set; }
}
