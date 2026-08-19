namespace Maktab.Application.Abstractions;

public interface ITextbookService
{
    Task<IReadOnlyList<TextbookDto>> GetTextbooksAsync(CancellationToken cancellationToken = default);
    Task<int> AddTextbookAsync(SaveTextbookDto textbook, CancellationToken cancellationToken = default);
    Task UpdateTextbookAsync(int textbookId, SaveTextbookDto textbook, CancellationToken cancellationToken = default);
    Task DeleteTextbookAsync(int textbookId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TextbookIssueDto>> GetIssuesAsync(CancellationToken cancellationToken = default);
    Task<int> IssueTextbookAsync(IssueTextbookDto issue, CancellationToken cancellationToken = default);
    Task ReturnTextbookAsync(ReturnTextbookDto returnInfo, CancellationToken cancellationToken = default);
}
