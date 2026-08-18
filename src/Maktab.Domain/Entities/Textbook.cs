namespace Maktab.Domain.Entities;

public sealed class Textbook
{
    public int TextbookId { get; set; }
    public required string Title { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    public string GradeLevel { get; set; } = string.Empty;
    public int TotalCopies { get; set; }
    public int AvailableCopies { get; set; }
}
