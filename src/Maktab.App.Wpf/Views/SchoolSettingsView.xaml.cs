using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Maktab.Application.Abstractions;
using Maktab.Infrastructure.Persistence;

namespace Maktab.App.Wpf.Views;

public partial class SchoolSettingsView : UserControl
{
    private readonly ISchoolSettingsService _settingsService;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUserService;
    private readonly AppFolders _appFolders;

    public SchoolSettingsView(
        ISchoolSettingsService settingsService,
        IAuditService auditService,
        ICurrentUserService currentUserService,
        AppFolders appFolders)
    {
        _settingsService = settingsService;
        _auditService = auditService;
        _currentUserService = currentUserService;
        _appFolders = appFolders;

        InitializeComponent();
        Loaded += SchoolSettingsView_Loaded;
    }

    private async void SchoolSettingsView_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadSettingsAsync();
    }

    private async Task LoadSettingsAsync()
    {
        try
        {
            var settings = await _settingsService.GetSettingsAsync();
            SchoolNameTextBox.Text = settings.SchoolName;
            SchoolAddressTextBox.Text = settings.SchoolAddress;
            PhoneNumberTextBox.Text = settings.PhoneNumber;
            AcademicYearTextBox.Text = settings.AcademicYear;
            LogoPathTextBox.Text = settings.LogoPath ?? string.Empty;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطا در بارگذاری تنظیمات:\n{ex.Message}", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BrowseLogoButton_Click(object sender, RoutedEventArgs e)
    {
        var openFileDialog = new OpenFileDialog
        {
            Filter = "Image Files (*.png;*.jpg;*.jpeg;*.bmp;*.gif)|*.png;*.jpg;*.jpeg;*.bmp;*.gif|All Files (*.*)|*.*",
            Title = "انتخاب تصویر لوگو"
        };

        if (openFileDialog.ShowDialog() == true)
        {
            LogoPathTextBox.Text = openFileDialog.FileName;
        }
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(SchoolNameTextBox.Text))
        {
            MessageBox.Show("نام مکتب الزامی است.", "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
            SchoolNameTextBox.Focus();
            return;
        }

        try
        {
            var settings = new SchoolSettingsDto
            {
                SchoolName = SchoolNameTextBox.Text.Trim(),
                SchoolAddress = SchoolAddressTextBox.Text.Trim(),
                PhoneNumber = PhoneNumberTextBox.Text.Trim(),
                AcademicYear = AcademicYearTextBox.Text.Trim(),
                LogoPath = CopyLogoIfNeeded(LogoPathTextBox.Text.Trim())
            };

            await _settingsService.SaveSettingsAsync(settings);
            await LogAuditAsync("ذخیره تنظیمات عمومی مکتب");
            MessageBox.Show("تنظیمات با موفقیت ذخیره شد.", "موفق", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private string? CopyLogoIfNeeded(string sourcePath)
    {
        if (string.IsNullOrWhiteSpace(sourcePath))
            return null;

        try
        {
            var logosDir = Path.Combine(_appFolders.Root, "Logos");
            Directory.CreateDirectory(logosDir);

            var extension = Path.GetExtension(sourcePath);
            var fileName = $"school_logo_{DateTime.Now:yyyyMMddHHmmss}{extension}";
            var destPath = Path.Combine(logosDir, fileName);

            // Copy only if source is not already inside Logos folder
            if (!string.Equals(Path.GetFullPath(sourcePath), Path.GetFullPath(destPath), StringComparison.OrdinalIgnoreCase))
            {
                File.Copy(sourcePath, destPath, overwrite: true);
            }

            return destPath;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطا در کپی لوگو:\n{ex.Message}", "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
            return null;
        }
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
            // Audit logging should not break save
        }
    }
}
