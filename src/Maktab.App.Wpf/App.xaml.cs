using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Windows;
using Maktab.Application;
using Maktab.Infrastructure;
using Maktab.Infrastructure.Persistence;

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
				services.AddSingleton<MainWindow>();
			})
			.Build();

		await _host.StartAsync();

		var appFolders = _host.Services.GetRequiredService<AppFolders>();
		DirectoryBootstrapper.EnsureFoldersExist(appFolders);

		var databaseInitializer = _host.Services.GetRequiredService<IDatabaseInitializer>();
		await databaseInitializer.InitializeAsync();

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

