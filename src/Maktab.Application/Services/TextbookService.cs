using Maktab.Application.Abstractions;
using Maktab.Domain.Entities;
using Maktab.Domain.Enums;

namespace Maktab.Application.Services;

public sealed class TextbookService(ITextbookRepository repository) : ITextbookService
{
    public async Task<IReadOnlyList<TextbookDto>> GetTextbooksAsync(CancellationToken cancellationToken = default)
    {
        var textbooks = await repository.GetTextbooksAsync(cancellationToken);
        return textbooks.Select(t => new TextbookDto
        {
            TextbookId = t.TextbookId,
            Title = t.Title,
            Subject = t.Subject,
            ClassId = t.ClassId,
            TotalCopies = t.TotalCopies,
            AvailableCopies = t.AvailableCopies
        }).ToList();
    }

    public async Task<int> AddTextbookAsync(SaveTextbookDto textbook, CancellationToken cancellationToken = default)
    {
        ValidateTextbook(textbook);

        var entity = new Textbook
        {
            Title = textbook.Title.Trim(),
            Subject = string.IsNullOrWhiteSpace(textbook.Subject) ? null : textbook.Subject.Trim(),
            ClassId = textbook.ClassId,
            TotalCopies = textbook.TotalCopies,
            AvailableCopies = textbook.TotalCopies
        };

        return await repository.CreateTextbookAsync(entity, cancellationToken);
    }

    public async Task UpdateTextbookAsync(int textbookId, SaveTextbookDto textbook, CancellationToken cancellationToken = default)
    {
        if (textbookId <= 0) throw new ArgumentOutOfRangeException(nameof(textbookId));
        ValidateTextbook(textbook);

        var existing = await repository.GetTextbookByIdAsync(textbookId, cancellationToken);
        if (existing is null) throw new InvalidOperationException("Textbook not found.");

        var issuedCopies = existing.TotalCopies - existing.AvailableCopies;
        if (textbook.TotalCopies < issuedCopies)
        {
            throw new InvalidOperationException($"Total copies cannot be less than currently issued copies ({issuedCopies}).");
        }

        var entity = new Textbook
        {
            TextbookId = textbookId,
            Title = textbook.Title.Trim(),
            Subject = string.IsNullOrWhiteSpace(textbook.Subject) ? null : textbook.Subject.Trim(),
            ClassId = textbook.ClassId,
            TotalCopies = textbook.TotalCopies,
            AvailableCopies = textbook.TotalCopies - issuedCopies
        };

        await repository.UpdateTextbookAsync(entity, cancellationToken);
    }

    public async Task DeleteTextbookAsync(int textbookId, CancellationToken cancellationToken = default)
    {
        if (textbookId <= 0) throw new ArgumentOutOfRangeException(nameof(textbookId));

        var textbook = await repository.GetTextbookByIdAsync(textbookId, cancellationToken);
        if (textbook is null) throw new InvalidOperationException("Textbook not found.");

        if (textbook.AvailableCopies != textbook.TotalCopies)
            throw new InvalidOperationException("Cannot delete a textbook with active issues.");

        await repository.DeleteTextbookAsync(textbookId, cancellationToken);
    }

    public Task<IReadOnlyList<TextbookIssueDto>> GetIssuesAsync(CancellationToken cancellationToken = default)
    {
        return repository.GetIssuesAsync(cancellationToken);
    }

    public async Task<int> IssueTextbookAsync(IssueTextbookDto issue, CancellationToken cancellationToken = default)
    {
        if (issue.TextbookId <= 0) throw new ArgumentOutOfRangeException(nameof(issue.TextbookId));
        if (issue.StudentId <= 0) throw new ArgumentOutOfRangeException(nameof(issue.StudentId));

        var textbook = await repository.GetTextbookByIdAsync(issue.TextbookId, cancellationToken);
        if (textbook is null) throw new InvalidOperationException("Textbook not found.");

        if (textbook.AvailableCopies <= 0) throw new InvalidOperationException("No available copies.");

        var entity = new TextbookIssue
        {
            TextbookId = issue.TextbookId,
            StudentId = issue.StudentId,
            IssueDate = DateTime.Today,
            Status = TextbookIssueStatus.Issued
        };

        return await repository.IssueTextbookAsync(entity, cancellationToken);
    }

    public async Task ReturnTextbookAsync(ReturnTextbookDto returnInfo, CancellationToken cancellationToken = default)
    {
        if (returnInfo.IssueId <= 0) throw new ArgumentOutOfRangeException(nameof(returnInfo.IssueId));
        await repository.ReturnTextbookAsync(returnInfo.IssueId, DateTime.Today, cancellationToken);
    }

    private static void ValidateTextbook(SaveTextbookDto textbook)
    {
        if (string.IsNullOrWhiteSpace(textbook.Title))
            throw new ArgumentException("Title is required.");

        if (textbook.TotalCopies <= 0)
            throw new ArgumentOutOfRangeException(nameof(textbook.TotalCopies), "Total copies must be greater than zero.");
    }
}
