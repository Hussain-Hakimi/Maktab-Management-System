using Maktab.Domain.Entities;

namespace Maktab.Application.Abstractions;

public interface ITextbookRepository
{
    Task<IReadOnlyList<Textbook>> GetTextbooksAsync(CancellationToken cancellationToken = default);
    Task<Textbook?> GetTextbookByIdAsync(int textbookId, CancellationToken cancellationToken = default);
    Task<int> CreateTextbookAsync(Textbook textbook, CancellationToken cancellationToken = default);
    Task UpdateTextbookAsync(Textbook textbook, CancellationToken cancellationToken = default);
    Task DeleteTextbookAsync(int textbookId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TextbookIssueDto>> GetIssuesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TextbookIssueDto>> GetActiveIssuesAsync(CancellationToken cancellationToken = default);

    Task<int> IssueTextbookAsync(TextbookIssue issue, CancellationToken cancellationToken = default);
    Task ReturnTextbookAsync(int issueId, DateTime returnDate, CancellationToken cancellationToken = default);
}
