using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Maktab.Application.Abstractions;

namespace Maktab.App.Wpf.Views;

public partial class AcademicYearView : UserControl
{
    private readonly IAcademicYearService _academicYearService;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ObservableCollection<AcademicYearDto> _years = [];

    public AcademicYearView(
        IAcademicYearService academicYearService,
        IAuditService auditService,
        ICurrentUserService currentUserService)
    {
        _academicYearService = academicYearService;
        _auditService = auditService;
        _currentUserService = currentUserService;

        InitializeComponent();
        YearsDataGrid.ItemsSource = _years;
        Loaded += AcademicYearView_Loaded;
    }

    private async void AcademicYearView_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadYearsAsync();
    }

    private async Task LoadYearsAsync()
    {
        try
        {
            var years = await _academicYearService.GetAllAcademicYearsAsync();
            _years.Clear();
            foreach (var year in years)
                _years.Add(year);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطا در بارگذاری سال‌ها:\n{ex.Message}", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void AddYearButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(YearNameTextBox.Text))
        {
            MessageBox.Show("نام سال الزامی است.", "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
            YearNameTextBox.Focus();
            return;
        }

        if (StartDatePicker.SelectedDate is not DateTime startDate)
        {
            MessageBox.Show("تاریخ شروع را انتخاب کنید.", "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (EndDatePicker.SelectedDate is not DateTime endDate)
        {
            MessageBox.Show("تاریخ پایان را انتخاب کنید.", "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (startDate >= endDate)
        {
            MessageBox.Show("تاریخ شروع باید قبل از تاریخ پایان باشد.", "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            await _academicYearService.CreateAcademicYearAsync(new SaveAcademicYearDto(
                YearName: YearNameTextBox.Text.Trim(),
                StartDate: startDate,
                EndDate: endDate));

            await LogAuditAsync($"افزودن سال تعلیمی '{YearNameTextBox.Text.Trim()}'");
            ClearForm();
            await LoadYearsAsync();
            MessageBox.Show("سال تعلیمی با موفقیت اضافه شد.", "موفق", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void SetActiveButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is int yearId)
        {
            try
            {
                await _academicYearService.SetActiveAcademicYearAsync(yearId);
                await LogAuditAsync($"تغییر سال تعلیمی فعال به {yearId}");
                await LoadYearsAsync();
                MessageBox.Show("سال تعلیمی فعال تغییر کرد.", "موفق", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        await LoadYearsAsync();
    }

    private void ClearForm()
    {
        YearNameTextBox.Clear();
        StartDatePicker.SelectedDate = null;
        EndDatePicker.SelectedDate = null;
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
            // Audit logging should not break year management
        }
    }
}
