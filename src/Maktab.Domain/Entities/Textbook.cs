namespace Maktab.Domain.Entities;

public sealed class Textbook
{
    public int TextbookId { get; set; }
    public required string Title { get; set; }
    public string? Subject { get; set; }
    public int? ClassId { get; set; }
    public int TotalCopies { get; set; }
    public int AvailableCopies { get; set; }
}
