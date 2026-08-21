namespace Maktab.Application.Abstractions;

public interface ISchoolSettingsService
{
    Task<SchoolSettingsDto> GetSettingsAsync(CancellationToken cancellationToken = default);
    Task SaveSettingsAsync(SchoolSettingsDto settings, CancellationToken cancellationToken = default);
}
