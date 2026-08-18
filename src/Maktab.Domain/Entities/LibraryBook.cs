namespace Maktab.Domain.Entities;

public sealed class LibraryBook
{
    public int BookId { get; set; }
    public required string Title { get; set; }
    public string Author { get; set; } = string.Empty;
    public string? Category { get; set; }
    public int TotalCopies { get; set; }
    public int AvailableCopies { get; set; }
}
