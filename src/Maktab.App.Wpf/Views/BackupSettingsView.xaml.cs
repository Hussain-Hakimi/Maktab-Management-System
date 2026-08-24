using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Maktab.Application.Abstractions;
using Maktab.Infrastructure.Persistence;

namespace Maktab.App.Wpf.Views;

public partial class BackupSettingsView : UserControl
{
    private readonly IBackupService _backupService;
    private readonly IAppLogger _logger;
    private readonly AppFolders _appFolders;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUserService;

    private readonly ObservableCollection<BackupInfoDto> _backups = [];

    public BackupSettingsView(
        IBackupService backupService,
        IAppLogger logger,
        AppFolders appFolders,
        IAuditService auditService,
        ICurrentUserService currentUserService)
    {
        _backupService = backupService;
        _logger = logger;
        _appFolders = appFolders;
        _auditService = auditService;
        _currentUserService = currentUserService;

        InitializeComponent();

        BackupsDataGrid.ItemsSource = _backups;
        Loaded += BackupSettingsView_Loaded;
    }

    private async void BackupSettingsView_Loaded(object sender, RoutedEventArgs e)
    {
        await RefreshBackupsAsync();
        await RefreshLogsAsync();
    }

    public async Task InitializeDataAsync()
    {
        await RefreshBackupsAsync();
        await RefreshLogsAsync();
    }

    private async Task RefreshBackupsAsync()
    {
        try
        {
            var backups = await _backupService.GetBackupsListAsync();
            _backups.Clear();
            foreach (var b in backups)
            {
                _backups.Add(b);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error loading backups list: {ex.Message}", ex);
            MessageBox.Show($"خطا در دریافت لیست پشتیبان‌ها:\n{ex.Message}", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task RefreshLogsAsync()
    {
        try
        {
            var lines = await _logger.ReadRecentLogsAsync(200);
            LogsTextBox.Text = lines.Count > 0 ? string.Join(Environment.NewLine, lines) : "(هنوز هیچ واقعه یا خطایی ثبت نشده است)";
        }
        catch (Exception ex)
        {
            LogsTextBox.Text = $"خطا در خواندن فایل لاگ: {ex.Message}";
        }
    }

    private async void CreateBackupButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var path = await _backupService.CreateBackupAsync();
            await LogAuditAsync("تهیه نسخه پشتیبان دستی");
            StatusTextBlock.Text = $"✅ نسخه پشتیبان جدید ایجاد شد: {Path.GetFileName(path)} ({DateTime.Now:HH:mm:ss})";
            await RefreshBackupsAsync();
            await RefreshLogsAsync();

            MessageBox.Show($"نسخه پشتیبان با موفقیت تهیه شد.\nمسیر فایل:\n{path}", "پشتیبان‌گیری موفق", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطا در تهیه نسخه پشتیبان:\n{ex.Message}", "خطا در پشتیبان‌گیری", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void CopyBackupToUsbButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Get USB drives
            var usbDrives = await _backupService.GetRemovableDrivePathsAsync();
            if (usbDrives.Count == 0)
            {
                MessageBox.Show("هیچ فلش USB متصل و آماده‌ای شناسایی نشد.", "فلش یافت نشد", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Get latest backup
            var backups = await _backupService.GetBackupsListAsync();
            if (backups.Count == 0)
            {
                MessageBox.Show("هیچ نسخه پشتیبانی وجود ندارد. ابتدا یک نسخه پشتیبان تهیه کنید.", "بدون پشتیبان", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var latestBackup = backups[0];
            var destinationDir = Path.Combine(usbDrives[0], "MaktabBackups");
            Directory.CreateDirectory(destinationDir);
            var destinationPath = Path.Combine(destinationDir, latestBackup.FileName);

            File.Copy(latestBackup.FilePath, destinationPath, overwrite: true);

            await LogAuditAsync($"کپی پشتیبان به فلش USB: {Path.GetFileName(destinationPath)}");
            StatusTextBlock.Text = $"✅ نسخه پشتیبان به فلش USB کپی شد: {destinationPath}";

            MessageBox.Show($"نسخه پشتیبان با موفقیت به فلش USB کپی شد.\nمسیر مقصد:\n{destinationPath}", "کپی موفق", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطا در کپی به فلش USB:\n{ex.Message}", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void RestoreSelectedButton_Click(object sender, RoutedEventArgs e)
    {
        if (BackupsDataGrid.SelectedItem is not BackupInfoDto selected)
        {
            MessageBox.Show("لطفاً یک فایل پشتیبان را از جدول انتخاب نمایید.", "فایل انتخاب نشده", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var confirm = MessageBox.Show(
            $"آیا از بازیابی دیتابیس از فایل «{selected.FileName}» اطمینان دارید؟\nتمام تغییرات پس از این تاریخ بازنویسی خواهند شد.",
            "تأیید بازیابی اطلاعات",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirm != MessageBoxResult.Yes)
        {
            return;
        }

        await ExecuteRestoreAsync(selected.FilePath);
    }

    private async void BrowseAndRestoreButton_Click(object sender, RoutedEventArgs e)
    {
        var openFileDialog = new OpenFileDialog
        {
            Filter = "SQLite Database Files (*.db)|*.db|All Files (*.*)|*.*",
            Title = "انتخاب فایل دیتابیس نسخه پشتیبان"
        };

        if (openFileDialog.ShowDialog() == true)
        {
            var confirm = MessageBox.Show(
                $"آیا از بازیابی دیتابیس از فایل «{openFileDialog.FileName}» اطمینان دارید؟",
                "تأیید بازیابی اطلاعات",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm == MessageBoxResult.Yes)
            {
                await ExecuteRestoreAsync(openFileDialog.FileName);
            }
        }
    }

    private async Task ExecuteRestoreAsync(string filePath)
    {
        try
        {
            await _backupService.RestoreBackupAsync(filePath);
            await LogAuditAsync($"بازیابی دیتابیس از فایل {Path.GetFileName(filePath)}");
            StatusTextBlock.Text = $"✅ دیتابیس با موفقیت از «{Path.GetFileName(filePath)}» بازیابی شد. لطفاً برنامه را مجدداً بارگذاری کنید.";
            await RefreshBackupsAsync();
            await RefreshLogsAsync();

            MessageBox.Show("دیتابیس با موفقیت بازیابی شد.\nتوصیه می‌شود جهت هماهنگی کامل فرم‌ها، برنامه را یک‌بار ببندید و دوباره باز کنید.", "بازیابی موفق", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطا در بازیابی دیتابیس:\n{ex.Message}", "خطا در بازیابی", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OpenBackupsFolderButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Directory.CreateDirectory(_appFolders.Backups);
            Process.Start(new ProcessStartInfo("explorer.exe", _appFolders.Backups) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"امکان باز کردن پوشه وجود ندارد:\n{ex.Message}", "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void RefreshBackupsButton_Click(object sender, RoutedEventArgs e)
    {
        await RefreshBackupsAsync();
    }

    private async void RefreshLogsButton_Click(object sender, RoutedEventArgs e)
    {
        await RefreshLogsAsync();
    }

    private async Task LogAuditAsync(string action)
    {
        try
        {
            var userName = _currentUserService.CurrentUser?.Username ?? "Unknown";
            await _auditService.LogAsync(userName, action);
        }
        catch
        {
            // Audit logging should not break backup operations
        }
    }
}
