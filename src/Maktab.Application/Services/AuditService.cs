using Maktab.Application.Abstractions;
using Maktab.Domain.Entities;

namespace Maktab.Application.Services;

public sealed class AuditService(IAuditLogRepository repository) : IAuditService
{
    public async Task LogAsync(string userName, string action, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userName))
            throw new ArgumentException("User name is required.", nameof(userName));
        if (string.IsNullOrWhiteSpace(action))
            throw new ArgumentException("Action is required.", nameof(action));

        var log = new AuditLog
        {
            UserName = userName.Trim(),
            Action = action.Trim(),
            Timestamp = DateTime.Now
        };

        await repository.AddAsync(log, cancellationToken);
    }

    public async Task<IReadOnlyList<AuditLogDto>> GetRecentLogsAsync(int maxRows = 100, CancellationToken cancellationToken = default)
    {
        if (maxRows <= 0) maxRows = 100;

        var logs = await repository.GetRecentAsync(maxRows, cancellationToken);
        return logs.Select(l => new AuditLogDto
        {
            LogId = l.LogId,
            UserName = l.UserName,
            Action = l.Action,
            Timestamp = l.Timestamp
        }).ToList();
    }
}
