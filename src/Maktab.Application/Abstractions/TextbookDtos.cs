using Maktab.Domain.Enums;

namespace Maktab.Application.Abstractions;

public sealed class TextbookDto
{
    public int TextbookId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Subject { get; set; }
    public int? ClassId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public int TotalCopies { get; set; }
    public int AvailableCopies { get; set; }
}

public sealed class TextbookIssueDto
{
    public int IssueId { get; set; }
    public int TextbookId { get; set; }
    public string TextbookTitle { get; set; } = string.Empty;
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string RollNumber { get; set; } = string.Empty;
    public DateTime IssueDate { get; set; }
    public DateTime? ReturnDate { get; set; }
    public TextbookIssueStatus Status { get; set; }
}

public sealed record SaveTextbookDto(
    string Title,
    string? Subject,
    int? ClassId,
    int TotalCopies);

public sealed record IssueTextbookDto(
    int TextbookId,
    int StudentId);

public sealed record ReturnTextbookDto(
    int IssueId);
