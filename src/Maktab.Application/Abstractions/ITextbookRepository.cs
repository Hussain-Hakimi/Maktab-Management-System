using Maktab.Domain.Entities;

namespace Maktab.Application.Abstractions;

public interface ITextbookRepository
{
    Task<IReadOnlyList<Textbook>> GetTextbooksAsync(CancellationToken cancellationToken = default);
    Task<Textbook?> GetTextbookByIdAsync(int textbookId, CancellationToken cancellationToken = default);
    Task<int> CreateTextbookAsync(Textbook textbook, CancellationToken cancellationToken = default);
    Task UpdateTextbookAsync(Textbook textbook, CancellationToken cancellationToken = default);
    Task DeleteTextbookAsync(int textbookId, CancellationToken cancellationToken = default);
    Task AdjustAvailableCopiesAsync(int textbookId, int delta, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TextbookIssue>> GetIssuesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TextbookIssue>> GetIssuesByTextbookAsync(int textbookId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TextbookIssue>> GetActiveIssuesAsync(CancellationToken cancellationToken = default);
    Task<TextbookIssue?> GetIssueByIdAsync(int issueId, CancellationToken cancellationToken = default);
    Task<int> CreateIssueAsync(TextbookIssue issue, CancellationToken cancellationToken = default);
    Task MarkIssueReturnedAsync(int issueId, DateOnly returnDate, CancellationToken cancellationToken = default);
}
