using Maktab.Application.Abstractions;
using Maktab.Domain.Entities;
using Maktab.Domain.Rules;

namespace Maktab.Application.Services;

public sealed class SettingService(ISettingRepository repository) : ISettingService
{
    private const string PassingAverageKey = "Promotion.PassingAverage";
    private const string PassingMarkKey = "Promotion.PassingMark";
    private const string MaxFailedSubjectsKey = "Promotion.MaxFailedSubjects";
    private const string MaxAbsenceDaysKey = "Promotion.MaxAbsenceDays";

    public async Task<PromotionSettingsDto> GetPromotionSettingsAsync(CancellationToken cancellationToken = default)
    {
        var settings = await repository.GetAllAsync(cancellationToken);
        var dict = settings.ToDictionary(s => s.Key, s => s.Value);

        return new PromotionSettingsDto
        {
            PassingAverage = GetDecimal(dict, PassingAverageKey, 65m),
            PassingMark = GetDecimal(dict, PassingMarkKey, 40m),
            MaxAllowedFailedSubjects = GetInt(dict, MaxFailedSubjectsKey, 3),
            MaxAllowedAbsenceDays = GetInt(dict, MaxAbsenceDaysKey, 30)
        };
    }

    public async Task SavePromotionSettingsAsync(PromotionSettingsDto settings, CancellationToken cancellationToken = default)
    {
        if (settings.PassingAverage < 0m || settings.PassingAverage > 100m)
            throw new ArgumentOutOfRangeException(nameof(settings.PassingAverage), "Passing average must be between 0 and 100.");
        if (settings.PassingMark < 0m || settings.PassingMark > 100m)
            throw new ArgumentOutOfRangeException(nameof(settings.PassingMark), "Passing mark must be between 0 and 100.");
        if (settings.MaxAllowedFailedSubjects < 0)
            throw new ArgumentOutOfRangeException(nameof(settings.MaxAllowedFailedSubjects), "Max failed subjects cannot be negative.");
        if (settings.MaxAllowedAbsenceDays < 0)
            throw new ArgumentOutOfRangeException(nameof(settings.MaxAllowedAbsenceDays), "Max absence days cannot be negative.");

        await repository.UpsertAsync(new Setting { Key = PassingAverageKey, Value = settings.PassingAverage.ToString("0.##") }, cancellationToken);
        await repository.UpsertAsync(new Setting { Key = PassingMarkKey, Value = settings.PassingMark.ToString("0.##") }, cancellationToken);
        await repository.UpsertAsync(new Setting { Key = MaxFailedSubjectsKey, Value = settings.MaxAllowedFailedSubjects.ToString() }, cancellationToken);
        await repository.UpsertAsync(new Setting { Key = MaxAbsenceDaysKey, Value = settings.MaxAllowedAbsenceDays.ToString() }, cancellationToken);

        // Update static promotion policy so changes take effect immediately
        PromotionPolicy.SetValues(
            settings.PassingAverage,
            settings.PassingMark,
            settings.MaxAllowedFailedSubjects,
            settings.MaxAllowedAbsenceDays);
    }

    private static decimal GetDecimal(Dictionary<string, string> dict, string key, decimal defaultValue)
    {
        return dict.TryGetValue(key, out var value) && decimal.TryParse(value, out var result) ? result : defaultValue;
    }

    private static int GetInt(Dictionary<string, string> dict, string key, int defaultValue)
    {
        return dict.TryGetValue(key, out var value) && int.TryParse(value, out var result) ? result : defaultValue;
    }
}
