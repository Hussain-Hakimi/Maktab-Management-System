using Maktab.Domain.Entities;

namespace Maktab.Application.Abstractions;

public interface ISettingRepository
{
    Task<Setting?> GetByKeyAsync(string key, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Setting>> GetAllAsync(CancellationToken cancellationToken = default);
    Task UpsertAsync(Setting setting, CancellationToken cancellationToken = default);
}
