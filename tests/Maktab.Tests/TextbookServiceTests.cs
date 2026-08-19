using Maktab.Application.Abstractions;
using Maktab.Application.Services;
using Maktab.Domain.Entities;
using Maktab.Domain.Enums;

namespace Maktab.Tests;

public class TextbookServiceTests
{
    private sealed class InMemoryTextbookRepository : ITextbookRepository
    {
        private readonly List<Textbook> _textbooks = [];
        private readonly List<TextbookIssue> _issues = [];
        private int _nextTextbookId = 1;
        private int _nextIssueId = 1;

        public Task<IReadOnlyList<Textbook>> GetTextbooksAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Textbook>>(_textbooks.ToList());

        public Task<Textbook?> GetTextbookByIdAsync(int textbookId, CancellationToken cancellationToken = default)
            => Task.FromResult(_textbooks.FirstOrDefault(t => t.TextbookId == textbookId));

        public Task<int> CreateTextbookAsync(Textbook textbook, CancellationToken cancellationToken = default)
        {
            textbook.TextbookId = _nextTextbookId++;
            _textbooks.Add(textbook);
            return Task.FromResult(textbook.TextbookId);
        }

        public Task UpdateTextbookAsync(Textbook textbook, CancellationToken cancellationToken = default)
        {
            var idx = _textbooks.FindIndex(t => t.TextbookId == textbook.TextbookId);
            if (idx >= 0) _textbooks[idx] = textbook;
            return Task.CompletedTask;
        }

        public Task DeleteTextbookAsync(int textbookId, CancellationToken cancellationToken = default)
        {
            _textbooks.RemoveAll(t => t.TextbookId == textbookId);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<TextbookIssueDto>> GetIssuesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<TextbookIssueDto>>(_issues.Select(i => new TextbookIssueDto
            {
                IssueId = i.IssueId,
                TextbookId = i.TextbookId,
                StudentId = i.StudentId,
                IssueDate = i.IssueDate,
                ReturnDate = i.ReturnDate,
                Status = i.Status
            }).ToList());

        public Task<IReadOnlyList<TextbookIssueDto>> GetActiveIssuesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<TextbookIssueDto>>(_issues.Where(i => i.Status == TextbookIssueStatus.Issued).Select(i => new TextbookIssueDto
            {
                IssueId = i.IssueId,
                TextbookId = i.TextbookId,
                StudentId = i.StudentId,
                IssueDate = i.IssueDate,
                ReturnDate = i.ReturnDate,
                Status = i.Status
            }).ToList());

        public Task<int> IssueTextbookAsync(TextbookIssue issue, CancellationToken cancellationToken = default)
        {
            issue.IssueId = _nextIssueId++;
            _issues.Add(issue);
            var textbook = _textbooks.First(t => t.TextbookId == issue.TextbookId);
            textbook.AvailableCopies--;
            return Task.FromResult(issue.IssueId);
        }

        public Task ReturnTextbookAsync(int issueId, DateTime returnDate, CancellationToken cancellationToken = default)
        {
            var issue = _issues.First(i => i.IssueId == issueId);
            issue.ReturnDate = returnDate;
            issue.Status = TextbookIssueStatus.Returned;
            var textbook = _textbooks.First(t => t.TextbookId == issue.TextbookId);
            textbook.AvailableCopies++;
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task AddTextbook_WithValidData_ReturnsIdAndCopiesAvailableEqualToTotal()
    {
        var repo = new InMemoryTextbookRepository();
        var service = new TextbookService(repo);

        var id = await service.AddTextbookAsync(new SaveTextbookDto("Math", "Mathematics", 1, 5));

        Assert.True(id > 0);
        var textbooks = await service.GetTextbooksAsync();
        Assert.Single(textbooks);
        Assert.Equal(5, textbooks[0].TotalCopies);
        Assert.Equal(5, textbooks[0].AvailableCopies);
    }

    [Fact]
    public async Task IssueTextbook_DecrementsAvailableCopies()
    {
        var repo = new InMemoryTextbookRepository();
        var service = new TextbookService(repo);
        var textbookId = await service.AddTextbookAsync(new SaveTextbookDto("Math", "Mathematics", 1, 3));

        await service.IssueTextbookAsync(new IssueTextbookDto(textbookId, 1));

        var textbooks = await service.GetTextbooksAsync();
        Assert.Equal(2, textbooks[0].AvailableCopies);
    }

    [Fact]
    public async Task ReturnTextbook_IncrementsAvailableCopies()
    {
        var repo = new InMemoryTextbookRepository();
        var service = new TextbookService(repo);
        var textbookId = await service.AddTextbookAsync(new SaveTextbookDto("Math", "Mathematics", 1, 2));
        var issueId = await service.IssueTextbookAsync(new IssueTextbookDto(textbookId, 1));

        await service.ReturnTextbookAsync(new ReturnTextbookDto(issueId));

        var textbooks = await service.GetTextbooksAsync();
        Assert.Equal(2, textbooks[0].AvailableCopies);
        var issues = await service.GetIssuesAsync();
        Assert.Single(issues);
        Assert.Equal(TextbookIssueStatus.Returned, issues[0].Status);
    }

    [Fact]
    public async Task IssueTextbook_WhenNoAvailableCopies_ThrowsInvalidOperationException()
    {
        var repo = new InMemoryTextbookRepository();
        var service = new TextbookService(repo);
        var textbookId = await service.AddTextbookAsync(new SaveTextbookDto("Math", "Mathematics", 1, 1));

        await service.IssueTextbookAsync(new IssueTextbookDto(textbookId, 1));

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await service.IssueTextbookAsync(new IssueTextbookDto(textbookId, 2));
        });
    }
}
