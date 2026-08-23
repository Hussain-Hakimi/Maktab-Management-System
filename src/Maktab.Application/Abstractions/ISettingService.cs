namespace Maktab.Application.Abstractions;

public interface ISettingService
{
    Task<PromotionSettingsDto> GetPromotionSettingsAsync(CancellationToken cancellationToken = default);
    Task SavePromotionSettingsAsync(PromotionSettingsDto settings, CancellationToken cancellationToken = default);
}
