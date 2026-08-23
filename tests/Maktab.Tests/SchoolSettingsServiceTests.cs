using Maktab.Application.Abstractions;
using Maktab.Application.Services;
using Maktab.Domain.Entities;

namespace Maktab.Tests;

public class SchoolSettingsServiceTests
{
    private sealed class InMemorySettingRepository : ISettingRepository
    {
        private readonly Dictionary<string, string> _settings = new();

        public Task<Setting?> GetByKeyAsync(string key, CancellationToken cancellationToken = default)
            => Task.FromResult(_settings.TryGetValue(key, out var value) ? new Setting { Key = key, Value = value } : null);

        public Task<IReadOnlyList<Setting>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Setting>>(_settings.Select(kvp => new Setting { Key = kvp.Key, Value = kvp.Value }).ToList());

        public Task UpsertAsync(Setting setting, CancellationToken cancellationToken = default)
        {
            _settings[setting.Key] = setting.Value;
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task GetSettings_ReturnsDefaultsWhenEmpty()
    {
        var repo = new InMemorySettingRepository();
        var service = new SchoolSettingsService(repo);

        var settings = await service.GetSettingsAsync();

        Assert.Equal("مکتب نمونه", settings.SchoolName);
        Assert.Equal("", settings.SchoolAddress);
        Assert.Null(settings.LogoPath);
    }

    [Fact]
    public async Task SaveSettings_ThenGet_ReturnsSavedValues()
    {
        var repo = new InMemorySettingRepository();
        var service = new SchoolSettingsService(repo);

        var dto = new SchoolSettingsDto
        {
            SchoolName = "مکتب افغان",
            SchoolAddress = "کابل",
            PhoneNumber = "0700123456",
            AcademicYear = "۱۴۰۴ - ۱۴۰۵",
            LogoPath = "/logos/logo.png"
        };

        await service.SaveSettingsAsync(dto);
        var result = await service.GetSettingsAsync();

        Assert.Equal("مکتب افغان", result.SchoolName);
        Assert.Equal("کابل", result.SchoolAddress);
        Assert.Equal("/logos/logo.png", result.LogoPath);
    }

    [Fact]
    public async Task SaveSettings_WhenSchoolNameMissing_ThrowsArgumentException()
    {
        var repo = new InMemorySettingRepository();
        var service = new SchoolSettingsService(repo);

        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await service.SaveSettingsAsync(new SchoolSettingsDto { SchoolName = "" });
        });
    }
}
