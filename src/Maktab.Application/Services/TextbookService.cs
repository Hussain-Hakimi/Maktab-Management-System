using Maktab.Application.Abstractions;
using Maktab.Domain.Entities;

namespace Maktab.Application.Services;

public sealed class TextbookService(
    ITextbookRepository textbookRepository,
    IStudentRepository studentRepository) : ITextbookService
{
    public Task<IReadOnlyList<Textbook>> GetTextbooksAsync(CancellationToken cancellationToken = default)
    {
        return textbookRepository.GetTextbooksAsync(cancellationToken);
    }

    public Task<int> AddTextbookAsync(string title, string subjectName, string gradeLevel, int totalCopies, CancellationToken cancellationToken = default)
    {
        ValidateTextbook(title, totalCopies);

        var textbook = new Textbook
        {
            Title = title.Trim(),
            SubjectName = subjectName?.Trim() ?? string.Empty,
            GradeLevel = gradeLevel?.Trim() ?? string.Empty,
            TotalCopies = totalCopies,
            AvailableCopies = totalCopies
        };

        return textbookRepository.CreateTextbookAsync(textbook, cancellationToken);
    }

    public async Task UpdateTextbookAsync(int textbookId, string title, string subjectName, string gradeLevel, int totalCopies, CancellationToken cancellationToken = default)
    {
        if (textbookId <= 0) throw new ArgumentOutOfRangeException(nameof(textbookId));
        ValidateTextbook(title, totalCopies);

        var existing = await textbookRepository.GetTextbookByIdAsync(textbookId, cancellationToken);
        if (existing is null) throw new InvalidOperationException("Textbook not found.");

        var issuedOut = existing.TotalCopies - existing.AvailableCopies;
        if (totalCopies < issuedOut)
        {
            throw new InvalidOperationException($"Cannot set total copies to {totalCopies}: {issuedOut} copies are currently issued to students.");
        }

        var textbook = new Textbook
        {
            TextbookId = textbookId,
            Title = title.Trim(),
            SubjectName = subjectName?.Trim() ?? string.Empty,
            GradeLevel = gradeLevel?.Trim() ?? string.Empty,
            TotalCopies = totalCopies,
            AvailableCopies = totalCopies - issuedOut
        };

        await textbookRepository.UpdateTextbookAsync(textbook, cancellationToken);
    }

    public async Task DeleteTextbookAsync(int textbookId, CancellationToken cancellationToken = default)
    {
        if (textbookId <= 0) throw new ArgumentOutOfRangeException(nameof(textbookId));

        var issues = await textbookRepository.GetIssuesByTextbookAsync(textbookId, cancellationToken);
        if (issues.Count > 0)
        {
            throw new InvalidOperationException("This textbook has issue records and cannot be deleted.");
        }

        await textbookRepository.DeleteTextbookAsync(textbookId, cancellationToken);
    }

    public async Task<IReadOnlyList<TextbookIssueDto>> GetIssueHistoryAsync(CancellationToken cancellationToken = default)
    {
        var issues = await textbookRepository.GetIssuesAsync(cancellationToken);
        return await ToDtosAsync(issues, cancellationToken);
    }

    public async Task<IReadOnlyList<TextbookIssueDto>> GetActiveIssuesAsync(CancellationToken cancellationToken = default)
    {
        var issues = await textbookRepository.GetActiveIssuesAsync(cancellationToken);
        return await ToDtosAsync(issues, cancellationToken);
    }

    public async Task<int> IssueTextbookAsync(int textbookId, int studentId, CancellationToken cancellationToken = default)
    {
        if (textbookId <= 0) throw new ArgumentOutOfRangeException(nameof(textbookId));
        if (studentId <= 0) throw new ArgumentOutOfRangeException(nameof(studentId));

        var textbook = await textbookRepository.GetTextbookByIdAsync(textbookId, cancellationToken);
        if (textbook is null) throw new InvalidOperationException("Textbook not found.");
        if (textbook.AvailableCopies <= 0)
        {
            throw new InvalidOperationException($"No copies of '{textbook.Title}' are available right now.");
        }

        var student = await studentRepository.GetStudentByIdAsync(studentId, cancellationToken);
        if (student is null) throw new InvalidOperationException("Student not found.");

        var issue = new TextbookIssue
        {
            TextbookId = textbookId,
            StudentId = studentId,
            IssueDate = DateOnly.FromDateTime(DateTime.Today)
        };

        var issueId = await textbookRepository.CreateIssueAsync(issue, cancellationToken);
        await textbookRepository.AdjustAvailableCopiesAsync(textbookId, -1, cancellationToken);
        return issueId;
    }

    public async Task ReturnTextbookAsync(int issueId, CancellationToken cancellationToken = default)
    {
        if (issueId <= 0) throw new ArgumentOutOfRangeException(nameof(issueId));

        var issue = await textbookRepository.GetIssueByIdAsync(issueId, cancellationToken);
        if (issue is null) throw new InvalidOperationException("Issue record not found.");
        if (issue.IsReturned) throw new InvalidOperationException("This textbook has already been returned.");

        await textbookRepository.MarkIssueReturnedAsync(issueId, DateOnly.FromDateTime(DateTime.Today), cancellationToken);
        await textbookRepository.AdjustAvailableCopiesAsync(issue.TextbookId, +1, cancellationToken);
    }

    private async Task<IReadOnlyList<TextbookIssueDto>> ToDtosAsync(IReadOnlyList<TextbookIssue> issues, CancellationToken cancellationToken)
    {
        var textbooks = await textbookRepository.GetTextbooksAsync(cancellationToken);
        var textbookMap = textbooks.ToDictionary(t => t.TextbookId, t => t.Title);
        var students = await studentRepository.GetStudentsAsync(cancellationToken);
        var studentMap = students.ToDictionary(s => s.StudentId);

        return issues.Select(i =>
        {
            studentMap.TryGetValue(i.StudentId, out var student);
            return new TextbookIssueDto
            {
                IssueId = i.IssueId,
                TextbookId = i.TextbookId,
                TextbookTitle = textbookMap.TryGetValue(i.TextbookId, out var title) ? title : $"Textbook {i.TextbookId}",
                StudentId = i.StudentId,
                StudentName = student is null ? $"شاگرد {i.StudentId}" : $"{student.FirstName} {student.LastName}",
                RollNumber = student?.RollNumber ?? string.Empty,
                IssueDate = i.IssueDate,
                ReturnDate = i.ReturnDate
            };
        }).ToList();
    }

    private static void ValidateTextbook(string title, int totalCopies)
    {
        if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Textbook title is required.", nameof(title));
        if (totalCopies < 1) throw new ArgumentOutOfRangeException(nameof(totalCopies), "Total copies must be at least 1.");
    }
}
