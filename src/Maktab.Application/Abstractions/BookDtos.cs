using Maktab.Domain.Enums;

namespace Maktab.Application.Abstractions;

public sealed class BookDto
{
    public int BookId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string? ISBN { get; set; }
    public string? Category { get; set; }
    public int TotalCopies { get; set; }
    public int AvailableCopies { get; set; }
}

public sealed class BookIssueDto
{
    public int IssueId { get; set; }
    public int BookId { get; set; }
    public string BookTitle { get; set; } = string.Empty;
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string RollNumber { get; set; } = string.Empty;
    public DateTime IssueDate { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime? ReturnDate { get; set; }
    public BookIssueStatus Status { get; set; }
    public bool IsOverdue => ReturnDate is null && DueDate < DateTime.Today;
}

public sealed record SaveBookDto(
    string Title,
    string Author,
    string? ISBN,
    string? Category,
    int TotalCopies);

public sealed record IssueBookDto(
    int BookId,
    int StudentId,
    DateTime DueDate);

public sealed record ReturnBookDto(
    int IssueId);
