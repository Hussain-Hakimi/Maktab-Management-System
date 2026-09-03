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

        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        RegisterGlobalExceptionHandlers();

        try
        {
            _host = Host.CreateDefaultBuilder()
                .ConfigureServices(services =>
                {
                    services
                        .AddApplicationCore()
                        .AddInfrastructureCore();

                    services.AddSingleton<ClassSubjectView>();
                    services.AddSingleton<StudentManagementView>();
                    services.AddSingleton<MarksEntryView>();
                    services.AddSingleton<StudentGradesView>();
                    services.AddSingleton<AttendanceView>();
                    services.AddSingleton<AttendanceReportsView>();
                    services.AddSingleton<LibraryView>();
                    services.AddSingleton<TextbookView>();
                    services.AddSingleton<FeesView>();
                    services.AddSingleton<ReportCardsView>();
                    services.AddSingleton<ReportsView>();
                    services.AddSingleton<BackupSettingsView>();
                    services.AddSingleton<DashboardView>();
                    services.AddSingleton<AlertsView>();
                    services.AddSingleton<UserManagementView>();
                    services.AddSingleton<PromotionSettingsView>();
                    services.AddSingleton<BulkImportView>();
                    services.AddSingleton<AuditLogView>();
                    services.AddSingleton<SchoolSettingsView>();
                    services.AddSingleton<AcademicYearView>();
                    services.AddSingleton<PromotionHistoryView>();
                    services.AddSingleton<MainWindow>();
                    services.AddSingleton<TeacherAssignmentView>();
                    services.AddSingleton<TeacherMySubjectsView>();
                    services.AddSingleton<GuardianClassView>();
                })
                .Build();

            await _host.StartAsync();

            var appFolders = _host.Services.GetRequiredService<AppFolders>();
            DirectoryBootstrapper.EnsureFoldersExist(appFolders);

            var databaseInitializer = _host.Services.GetRequiredService<IDatabaseInitializer>();
            await databaseInitializer.InitializeAsync();

            var userService = _host.Services.GetRequiredService<IUserService>();
            var users = await userService.GetAllUsersAsync();

            if (users.Count == 0)
            {
                var setupWindow = new FirstRunAdminSetupWindow(userService);
                var setupResult = setupWindow.ShowDialog();

                if (setupResult != true || setupWindow.CreatedAdmin is null)
                {
                    Shutdown(0);
                    return;
                }
            }

            var loginWindow = new LoginWindow(userService);
            var loginResult = loginWindow.ShowDialog();

            if (loginResult != true || loginWindow.AuthenticatedUser is null)
            {
                Shutdown(0);
                return;
            }

            var auditService = _host.Services.GetRequiredService<IAuditService>();
            await auditService.LogAsync(loginWindow.AuthenticatedUser.Username, "ورود به سیستم");

            var currentUserService = _host.Services.GetRequiredService<ICurrentUserService>();
            currentUserService.CurrentUser = loginWindow.AuthenticatedUser;

            try
            {
                var mainWindow = _host.Services.GetRequiredService<MainWindow>();
                mainWindow.SetCurrentUser(loginWindow.AuthenticatedUser);
                MainWindow = mainWindow;
                ShutdownMode = ShutdownMode.OnMainWindowClose;
                mainWindow.Show();
            }
            catch (Exception ex)
            {
                LogException("Failed to show main window", ex);
                MessageBox.Show(
                    $"باز کردن پنجره اصلی با خطا مواجه شد:\n{ex.Message}",
                    "خطا",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Shutdown(1);
                return;
            }
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
    }

    private void RegisterGlobalExceptionHandlers()
    {
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

        AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
        {
            LogException("Unhandled non-UI exception", args.ExceptionObject as Exception);
        };

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
