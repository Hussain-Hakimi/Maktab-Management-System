using Maktab.Domain.Entities;

namespace Maktab.Application.Abstractions;

public interface ITextbookService
{
    Task<IReadOnlyList<Textbook>> GetAllTextbooksAsync(CancellationToken cancellationToken = default);
    Task<int> AddTextbookAsync(string title, string? subjectName, string? gradeLevel, int totalCopies, CancellationToken cancellationToken = default);
    Task UpdateTextbookAsync(int textbookId, string title, string? subjectName, string? gradeLevel, int totalCopies, CancellationToken cancellationToken = default);
    Task RemoveTextbookAsync(int textbookId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TextbookIssueDto>> GetAllIssuesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TextbookIssueDto>> GetActiveIssuesAsync(CancellationToken cancellationToken = default);
    Task<int> IssueTextbookAsync(int textbookId, int studentId, CancellationToken cancellationToken = default);
    Task ReturnTextbookAsync(int issueId, CancellationToken cancellationToken = default);
}
