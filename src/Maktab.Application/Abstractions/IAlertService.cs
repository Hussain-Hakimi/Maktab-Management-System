namespace Maktab.Application.Abstractions;

public interface IAlertService
{
    Task<IReadOnlyList<AlertItemDto>> GetAlertsAsync(CancellationToken cancellationToken = default);
}
