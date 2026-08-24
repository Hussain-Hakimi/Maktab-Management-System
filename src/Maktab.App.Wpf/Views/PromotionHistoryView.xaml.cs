using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Maktab.Application.Abstractions;

namespace Maktab.App.Wpf.Views;

public partial class PromotionHistoryView : UserControl
{
    private readonly IPromotionService _promotionService;
    private readonly IAcademicYearService _academicYearService;

    private readonly ObservableCollection<PromotionHistoryDto> _history = [];

    public PromotionHistoryView(
        IPromotionService promotionService,
        IAcademicYearService academicYearService)
    {
        _promotionService = promotionService;
        _academicYearService = academicYearService;

        InitializeComponent();
        HistoryDataGrid.ItemsSource = _history;
        Loaded += PromotionHistoryView_Loaded;
    }

    private async void PromotionHistoryView_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadYearsAsync();
    }

    private async Task LoadYearsAsync()
    {
        try
        {
            var years = await _academicYearService.GetAllAcademicYearsAsync();
            AcademicYearComboBox.ItemsSource = years;

            // Optionally select all years: we can add a dummy "All" item
            var allYears = new List<AcademicYearDto>
            {
                new() { AcademicYearId = 0, YearName = "همه سال‌ها" }
            };
            allYears.AddRange(years);
            AcademicYearComboBox.ItemsSource = allYears;
            AcademicYearComboBox.SelectedIndex = 0;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطا در بارگذاری سال‌ها:\n{ex.Message}", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void AcademicYearComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        await LoadHistoryAsync();
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        await LoadHistoryAsync();
    }

    private async Task LoadHistoryAsync()
    {
        try
        {
            int? yearId = null;
            if (AcademicYearComboBox.SelectedValue is int selectedId && selectedId > 0)
                yearId = selectedId;

            var history = await _promotionService.GetPromotionHistoryAsync(yearId);
            _history.Clear();
            foreach (var item in history)
                _history.Add(item);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطا در بارگذاری تاریخچه ارتقاء:\n{ex.Message}", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
