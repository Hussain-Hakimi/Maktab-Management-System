using Maktab.Application.Abstractions;
using Maktab.Domain.Entities;

namespace Maktab.Application.Services;

public sealed class TextbookService(
    ITextbookRepository textbookRepository,
    IStudentRepository studentRepository) : ITextbookService
{
    public Task<IReadOnlyList<Textbook>> GetAllTextbooksAsync(CancellationToken cancellationToken = default)
    {
        return textbookRepository.GetTextbooksAsync(cancellationToken);
    }

    public Task<int> AddTextbookAsync(string title, string? subjectName, string? gradeLevel, int totalCopies, CancellationToken cancellationToken = default)
    {
        ValidateTextbook(title, totalCopies);

        var textbook = new Textbook
        {
            Title = title.Trim(),
            SubjectName = string.IsNullOrWhiteSpace(subjectName) ? null : subjectName.Trim(),
            GradeLevel = string.IsNullOrWhiteSpace(gradeLevel) ? null : gradeLevel.Trim(),
            TotalCopies = totalCopies,
            AvailableCopies = totalCopies
        };

        return textbookRepository.CreateTextbookAsync(textbook, cancellationToken);
    }

    public async Task UpdateTextbookAsync(int textbookId, string title, string? subjectName, string? gradeLevel, int totalCopies, CancellationToken cancellationToken = default)
    {
        if (textbookId <= 0) throw new ArgumentOutOfRangeException(nameof(textbookId));
        ValidateTextbook(title, totalCopies);

        var existing = await textbookRepository.GetTextbookByIdAsync(textbookId, cancellationToken);
        if (existing is null) throw new InvalidOperationException("کتاب درسی یافت نشد.");

        var issuedCopies = existing.TotalCopies - existing.AvailableCopies;
        if (totalCopies < issuedCopies)
        {
            throw new InvalidOperationException($"تعداد کل نسخه‌ها نمی‌تواند از تعداد نسخه‌های توزیع‌شده ({issuedCopies}) کمتر باشد.");
        }

        existing.Title = title.Trim();
        existing.SubjectName = string.IsNullOrWhiteSpace(subjectName) ? null : subjectName.Trim();
        existing.GradeLevel = string.IsNullOrWhiteSpace(gradeLevel) ? null : gradeLevel.Trim();
        existing.TotalCopies = totalCopies;
        existing.AvailableCopies = totalCopies - issuedCopies;

        await textbookRepository.UpdateTextbookAsync(existing, cancellationToken);
    }

    public async Task RemoveTextbookAsync(int textbookId, CancellationToken cancellationToken = default)
    {
        if (textbookId <= 0) throw new ArgumentOutOfRangeException(nameof(textbookId));

        if (await textbookRepository.HasActiveIssuesForTextbookAsync(textbookId, cancellationToken))
        {
            throw new InvalidOperationException("این کتاب درسی نسخه‌های توزیع‌شده دارد و تا بازگشت آنها قابل حذف نیست.");
        }

        await textbookRepository.DeleteTextbookAsync(textbookId, cancellationToken);
    }

    public async Task<IReadOnlyList<TextbookIssueDto>> GetAllIssuesAsync(CancellationToken cancellationToken = default)
    {
        return await EnrichIssuesAsync(await textbookRepository.GetIssuesAsync(cancellationToken), cancellationToken);
    }

    public async Task<IReadOnlyList<TextbookIssueDto>> GetActiveIssuesAsync(CancellationToken cancellationToken = default)
    {
        return await EnrichIssuesAsync(await textbookRepository.GetActiveIssuesAsync(cancellationToken), cancellationToken);
    }

    public async Task<int> IssueTextbookAsync(int textbookId, int studentId, CancellationToken cancellationToken = default)
    {
        if (textbookId <= 0) throw new ArgumentOutOfRangeException(nameof(textbookId));
        if (studentId <= 0) throw new ArgumentOutOfRangeException(nameof(studentId));

        var textbook = await textbookRepository.GetTextbookByIdAsync(textbookId, cancellationToken);
        if (textbook is null) throw new InvalidOperationException("کتاب درسی یافت نشد.");
        if (textbook.AvailableCopies <= 0)
        {
            throw new InvalidOperationException($"نسخه‌ای از کتاب «{textbook.Title}» در انبار موجود نیست.");
        }

        var student = await studentRepository.GetStudentByIdAsync(studentId, cancellationToken);
        if (student is null) throw new InvalidOperationException($"شاگرد با آیدی {studentId} یافت نشد.");

        // A student should not receive the same textbook twice before returning it
        var studentIssues = await textbookRepository.GetIssuesByStudentAsync(studentId, cancellationToken);
        if (studentIssues.Any(i => i.TextbookId == textbookId && !i.IsReturned))
        {
            throw new InvalidOperationException($"کتاب «{textbook.Title}» قبلاً به این شاگرد داده شده و هنوز بازگردانده نشده است.");
        }

        var issue = new TextbookIssue
        {
            TextbookId = textbookId,
            StudentId = studentId,
            IssueDate = DateOnly.FromDateTime(DateTime.Now),
            ReturnDate = null
        };

        var issueId = await textbookRepository.CreateIssueAsync(issue, cancellationToken);
        await textbookRepository.SetAvailableCopiesAsync(textbookId, textbook.AvailableCopies - 1, cancellationToken);
        return issueId;
    }

    public async Task ReturnTextbookAsync(int issueId, CancellationToken cancellationToken = default)
    {
        if (issueId <= 0) throw new ArgumentOutOfRangeException(nameof(issueId));

        var issues = await textbookRepository.GetIssuesAsync(cancellationToken);
        var issue = issues.FirstOrDefault(i => i.IssueId == issueId);
        if (issue is null) throw new InvalidOperationException("رکورد توزیع یافت نشد.");
        if (issue.IsReturned) throw new InvalidOperationException("این کتاب قبلاً بازگردانده شده است.");

        var today = DateOnly.FromDateTime(DateTime.Now);
        await textbookRepository.ReturnIssueAsync(issueId, today, cancellationToken);

        var textbook = await textbookRepository.GetTextbookByIdAsync(issue.TextbookId, cancellationToken);
        if (textbook is not null)
        {
            await textbookRepository.SetAvailableCopiesAsync(textbook.TextbookId, Math.Min(textbook.AvailableCopies + 1, textbook.TotalCopies), cancellationToken);
        }
    }

    private async Task<IReadOnlyList<TextbookIssueDto>> EnrichIssuesAsync(IReadOnlyList<TextbookIssue> issues, CancellationToken cancellationToken)
    {
        var result = new List<TextbookIssueDto>();

        foreach (var issue in issues)
        {
            var textbook = await textbookRepository.GetTextbookByIdAsync(issue.TextbookId, cancellationToken);
            var student = await studentRepository.GetStudentByIdAsync(issue.StudentId, cancellationToken);

            result.Add(new TextbookIssueDto
            {
                IssueId = issue.IssueId,
                TextbookId = issue.TextbookId,
                TextbookTitle = textbook?.Title ?? $"کتاب {issue.TextbookId}",
                StudentId = issue.StudentId,
                StudentName = student is null ? $"شاگرد {issue.StudentId}" : $"{student.FirstName} {student.LastName}",
                RollNumber = student?.RollNumber ?? string.Empty,
                IssueDate = issue.IssueDate,
                ReturnDate = issue.ReturnDate,
                IsReturned = issue.IsReturned
            });
        }

        return result;
    }

    private static void ValidateTextbook(string title, int totalCopies)
    {
        if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("عنوان کتاب درسی ضروری است.", nameof(title));
        if (totalCopies < 0) throw new ArgumentOutOfRangeException(nameof(totalCopies), "تعداد نسخه‌ها نمی‌تواند منفی باشد.");
    }
}
