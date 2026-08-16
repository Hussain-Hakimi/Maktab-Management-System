using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Windows;
using Maktab.Application;
using Maktab.Application.Abstractions;
using Maktab.Infrastructure;
using Maktab.Infrastructure.Persistence;
using Maktab.App.Wpf.Views;

namespace Maktab.App.Wpf;

public partial class App : System.Windows.Application
{
	private IHost? _host;

	protected override async void OnStartup(StartupEventArgs e)
	{
		base.OnStartup(e);

		_host = Host.CreateDefaultBuilder()
			.ConfigureServices(services =>
			{
				services
					.AddApplicationCore()
					.AddInfrastructureCore();

				// Register all views as singletons (each tab hosts one instance)
				services.AddSingleton<ClassSubjectView>();
				services.AddSingleton<StudentManagementView>();
				services.AddSingleton<MarksEntryView>();
				services.AddSingleton<ReportCardsView>();
				services.AddSingleton<BackupSettingsView>();
				services.AddSingleton<MainWindow>();
			})
			.Build();

		await _host.StartAsync();

		var appFolders = _host.Services.GetRequiredService<AppFolders>();
		DirectoryBootstrapper.EnsureFoldersExist(appFolders);

		var databaseInitializer = _host.Services.GetRequiredService<IDatabaseInitializer>();
		await databaseInitializer.InitializeAsync();

		// Auto-backup on startup (fire-and-forget with logging)
		_ = Task.Run(async () =>
		{
			try
			{
				var backupService = _host.Services.GetRequiredService<IBackupService>();
				var logger = _host.Services.GetRequiredService<IAppLogger>();

				var path = await backupService.CreateBackupAsync();
				logger.LogInfo($"Startup auto-backup created: {path}");

				await backupService.PruneOldBackupsAsync(retentionDays: 7);
				logger.LogInfo("Old backups pruned (7-day retention).");
			}
			catch (Exception ex)
			{
				var logger = _host.Services.GetService<IAppLogger>();
				logger?.LogError($"Startup auto-backup failed: {ex.Message}", ex);
			}
		});

		var mainWindow = _host.Services.GetRequiredService<MainWindow>();
		mainWindow.Show();
	}

	protected override async void OnExit(ExitEventArgs e)
	{
		if (_host is not null)
		{
			await _host.StopAsync();
			_host.Dispose();
		}

		base.OnExit(e);
	}
}
