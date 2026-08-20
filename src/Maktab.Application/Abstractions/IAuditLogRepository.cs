using Maktab.Domain.Entities;

namespace Maktab.Application.Abstractions;

public interface IAuditLogRepository
{
    Task<IReadOnlyList<AuditLog>> GetRecentAsync(int maxRows, CancellationToken cancellationToken = default);
    Task<int> AddAsync(AuditLog log, CancellationToken cancellationToken = default);
}
