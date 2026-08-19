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

		RegisterGlobalExceptionHandlers();

		try
		{
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
					services.AddSingleton<AttendanceView>();
					services.AddSingleton<LibraryView>();
					services.AddSingleton<TextbookView>();
					services.AddSingleton<MainWindow>();
				})
				.Build();

			await _host.StartAsync();

			var appFolders = _host.Services.GetRequiredService<AppFolders>();
			DirectoryBootstrapper.EnsureFoldersExist(appFolders);

			var databaseInitializer = _host.Services.GetRequiredService<IDatabaseInitializer>();
			await databaseInitializer.InitializeAsync();
		}
		catch (Exception ex)
		{
			LogException("Application startup failed", ex);
			MessageBox.Show(
				$"برنامه به درستی راه‌اندازی نشد. لطفاً فایل لاگ را بررسی نمایید.\n\nجزئیات: {ex.Message}",
				"خطا در راه‌اندازی برنامه",
				MessageBoxButton.OK,
				MessageBoxImage.Error);
			Shutdown(1);
			return;
		}

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

	private void RegisterGlobalExceptionHandlers()
	{
		// UI-thread exceptions: log and keep the app alive with a friendly Dari message
		DispatcherUnhandledException += (sender, args) =>
		{
			LogException("Unhandled UI exception", args.Exception);
			MessageBox.Show(
				$"یک خطای غیرمنتظره رخ داد. برنامه به کار خود ادامه می‌دهد، اما عملیات فعلی کامل نشد.\n\nجزئیات: {args.Exception.Message}",
				"خطای غیرمنتظره",
				MessageBoxButton.OK,
				MessageBoxImage.Error);
			args.Handled = true;
		};

		// Non-UI thread exceptions: log before the runtime terminates the process
		AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
		{
			LogException("Unhandled non-UI exception", args.ExceptionObject as Exception);
		};

		// Unobserved task exceptions: log and mark observed so the GC does not crash the process
		TaskScheduler.UnobservedTaskException += (sender, args) =>
		{
			LogException("Unobserved task exception", args.Exception);
			args.SetObserved();
		};
	}

	private void LogException(string message, Exception? exception)
	{
		try
		{
			var logger = _host?.Services.GetService<IAppLogger>();
			logger?.LogError(message, exception);
		}
		catch
		{
			// Logging must never crash the application
		}
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
