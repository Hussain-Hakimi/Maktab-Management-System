using Maktab.Application.Abstractions;
using Maktab.Domain.Entities;

namespace Maktab.Application.Services;

public sealed class SchoolSettingsService(ISettingRepository repository) : ISchoolSettingsService
{
    private const string SchoolNameKey = "School.Name";
    private const string SchoolAddressKey = "School.Address";
    private const string PhoneNumberKey = "School.Phone";
    private const string AcademicYearKey = "School.AcademicYear";
    private const string LogoPathKey = "School.LogoPath";

    public async Task<SchoolSettingsDto> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        var settings = await repository.GetAllAsync(cancellationToken);
        var dict = settings.ToDictionary(s => s.Key, s => s.Value);

        return new SchoolSettingsDto
        {
            SchoolName = GetString(dict, SchoolNameKey, "مکتب نمونه"),
            SchoolAddress = GetString(dict, SchoolAddressKey, ""),
            PhoneNumber = GetString(dict, PhoneNumberKey, ""),
            AcademicYear = GetString(dict, AcademicYearKey, AcademicYearProvider.GetCurrentAcademicYear()),
            LogoPath = GetNullableString(dict, LogoPathKey)
        };
    }

    public async Task SaveSettingsAsync(SchoolSettingsDto settings, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(settings.SchoolName))
            throw new ArgumentException("School name is required.", nameof(settings.SchoolName));

        await repository.UpsertAsync(new Setting { Key = SchoolNameKey, Value = settings.SchoolName.Trim() }, cancellationToken);
        await repository.UpsertAsync(new Setting { Key = SchoolAddressKey, Value = settings.SchoolAddress?.Trim() ?? "" }, cancellationToken);
        await repository.UpsertAsync(new Setting { Key = PhoneNumberKey, Value = settings.PhoneNumber?.Trim() ?? "" }, cancellationToken);
        await repository.UpsertAsync(new Setting { Key = AcademicYearKey, Value = settings.AcademicYear.Trim() }, cancellationToken);
        await repository.UpsertAsync(new Setting { Key = LogoPathKey, Value = settings.LogoPath ?? "" }, cancellationToken);
    }

    private static string GetString(Dictionary<string, string> dict, string key, string defaultValue)
        => dict.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : defaultValue;

    private static string? GetNullableString(Dictionary<string, string> dict, string key)
        => dict.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : null;
}
