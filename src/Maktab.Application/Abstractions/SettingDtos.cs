namespace Maktab.Application.Abstractions;

public sealed class PromotionSettingsDto
{
    public decimal PassingAverage { get; set; }
    public decimal PassingMark { get; set; }
    public int MaxAllowedFailedSubjects { get; set; }
    public int MaxAllowedAbsenceDays { get; set; }
}

public sealed class SettingDto
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}
