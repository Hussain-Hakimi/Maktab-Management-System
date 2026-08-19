namespace Maktab.Domain.Entities;

public sealed class Book
{
    public int BookId { get; set; }
    public required string Title { get; set; }
    public required string Author { get; set; }
    public string? ISBN { get; set; }
    public string? Category { get; set; }
    public int TotalCopies { get; set; }
    public int AvailableCopies { get; set; }
}
