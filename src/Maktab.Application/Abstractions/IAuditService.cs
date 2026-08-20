namespace Maktab.Application.Abstractions;

public interface IAuditService
{
    Task LogAsync(string userName, string action, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AuditLogDto>> GetRecentLogsAsync(int maxRows = 100, CancellationToken cancellationToken = default);
}
