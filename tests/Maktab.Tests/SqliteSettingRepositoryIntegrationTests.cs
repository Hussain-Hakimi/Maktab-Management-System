using Microsoft.Data.Sqlite;
using Maktab.Domain.Entities;
using Maktab.Infrastructure.Persistence;

namespace Maktab.Tests;

public class SqliteSettingRepositoryIntegrationTests : IDisposable
{
    private readonly string _tempDir;
    private readonly AppFolders _folders;
    private readonly ConnectionStringProvider _connectionStringProvider;
    private readonly SqliteSettingRepository _settingRepository;

    public SqliteSettingRepositoryIntegrationTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "MaktabSettingTests_" + Guid.NewGuid());
        _folders = new AppFolders(
            Root: _tempDir,
            Data: Path.Combine(_tempDir, "Data"),
            Logs: Path.Combine(_tempDir, "Logs"),
            Backups: Path.Combine(_tempDir, "Backups"),
            Reports: Path.Combine(_tempDir, "Reports"),
            Logos: Path.Combine(_tempDir, "Logos"));

        DirectoryBootstrapper.EnsureFoldersExist(_folders);

        _connectionStringProvider = new ConnectionStringProvider(_folders);
        var initializer = new SqliteDatabaseInitializer(_connectionStringProvider);
        initializer.InitializeAsync().GetAwaiter().GetResult();

        _settingRepository = new SqliteSettingRepository(_connectionStringProvider);
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        try { Directory.Delete(_tempDir, recursive: true); } catch { }
    }

    [Fact]
    public async Task UpsertAndGetSetting_Works()
    {
        var key = "Test.Key";
        var value = "TestValue";

        await _settingRepository.UpsertAsync(new Setting { Key = key, Value = value });

        var setting = await _settingRepository.GetByKeyAsync(key);

        Assert.NotNull(setting);
        Assert.Equal(value, setting.Value);
    }

    [Fact]
    public async Task Upsert_WhenDuplicateKey_UpdatesValue()
    {
        var key = "Test.Key";
        await _settingRepository.UpsertAsync(new Setting { Key = key, Value = "OldValue" });
        await _settingRepository.UpsertAsync(new Setting { Key = key, Value = "NewValue" });

        var settings = await _settingRepository.GetAllAsync();
        var target = settings.Single(s => s.Key == key);

        Assert.Equal("NewValue", target.Value);
    }

    [Fact]
    public async Task DefaultSettings_AreSeeded()
    {
        var settings = await _settingRepository.GetAllAsync();

        Assert.Contains(settings, s => s.Key == "Promotion.PassingAverage");
        Assert.Contains(settings, s => s.Key == "Promotion.PassingMark");
        Assert.Contains(settings, s => s.Key == "Promotion.MaxFailedSubjects");
        Assert.Contains(settings, s => s.Key == "Promotion.MaxAbsenceDays");
        Assert.Contains(settings, s => s.Key == "School.Name");
    }
}
