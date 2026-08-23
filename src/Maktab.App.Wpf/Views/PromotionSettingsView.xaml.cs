using System.Windows;
using System.Windows.Controls;
using Maktab.Application.Abstractions;

namespace Maktab.App.Wpf.Views;

public partial class PromotionSettingsView : UserControl
{
    private readonly ISettingService _settingService;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUserService;

    public PromotionSettingsView(
        ISettingService settingService,
        IAuditService auditService,
        ICurrentUserService currentUserService)
    {
        _settingService = settingService;
        _auditService = auditService;
        _currentUserService = currentUserService;

        InitializeComponent();
        Loaded += PromotionSettingsView_Loaded;
    }

    private async void PromotionSettingsView_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadSettingsAsync();
    }

    private async Task LoadSettingsAsync()
    {
        try
        {
            var settings = await _settingService.GetPromotionSettingsAsync();

            PassingAverageTextBox.Text = settings.PassingAverage.ToString("0.##");
            PassingMarkTextBox.Text = settings.PassingMark.ToString("0.##");
            MaxFailedSubjectsTextBox.Text = settings.MaxAllowedFailedSubjects.ToString();
            MaxAbsenceDaysTextBox.Text = settings.MaxAllowedAbsenceDays.ToString();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطا در بارگذاری تنظیمات:\n{ex.Message}", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (!ValidateInputs(out var settings))
            return;

        try
        {
            await _settingService.SavePromotionSettingsAsync(settings);
            await LogAuditAsync("ذخیره تنظیمات ارتقاء");
            MessageBox.Show("تنظیمات با موفقیت ذخیره شد.", "موفق", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private bool ValidateInputs(out PromotionSettingsDto settings)
    {
        settings = null!;

        if (!decimal.TryParse(PassingAverageTextBox.Text.Trim(), out var passingAverage) || passingAverage < 0m || passingAverage > 100m)
        {
            MessageBox.Show("اوسط فیصدی باید عددی بین ۰ و ۱۰۰ باشد.", "ورودی نامعتبر", MessageBoxButton.OK, MessageBoxImage.Warning);
            PassingAverageTextBox.Focus();
            return false;
        }

        if (!decimal.TryParse(PassingMarkTextBox.Text.Trim(), out var passingMark) || passingMark < 0m || passingMark > 100m)
        {
            MessageBox.Show("نمره قبولی باید عددی بین ۰ و ۱۰۰ باشد.", "ورودی نامعتبر", MessageBoxButton.OK, MessageBoxImage.Warning);
            PassingMarkTextBox.Focus();
            return false;
        }

        if (!int.TryParse(MaxFailedSubjectsTextBox.Text.Trim(), out var maxFailed) || maxFailed < 0)
        {
            MessageBox.Show("حداکثر مضامین ناکام باید یک عدد صحیح نامنفی باشد.", "ورودی نامعتبر", MessageBoxButton.OK, MessageBoxImage.Warning);
            MaxFailedSubjectsTextBox.Focus();
            return false;
        }

        if (!int.TryParse(MaxAbsenceDaysTextBox.Text.Trim(), out var maxAbsence) || maxAbsence < 0)
        {
            MessageBox.Show("حداکثر ایام غیرحاضری باید یک عدد صحیح نامنفی باشد.", "ورودی نامعتبر", MessageBoxButton.OK, MessageBoxImage.Warning);
            MaxAbsenceDaysTextBox.Focus();
            return false;
        }

        settings = new PromotionSettingsDto
        {
            PassingAverage = passingAverage,
            PassingMark = passingMark,
            MaxAllowedFailedSubjects = maxFailed,
            MaxAllowedAbsenceDays = maxAbsence
        };

        return true;
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
            // Audit logging should not break settings save
        }
    }
}
