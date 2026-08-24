using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Maktab.Application.Abstractions;

namespace Maktab.App.Wpf.Views;

public partial class AlertsView : UserControl
{
    private readonly IAlertService _alertService;
    private readonly ObservableCollection<AlertItemDto> _alerts = [];

    public AlertsView(IAlertService alertService)
    {
        _alertService = alertService;
        InitializeComponent();
        AlertsDataGrid.ItemsSource = _alerts;
        Loaded += AlertsView_Loaded;
    }

    private async void AlertsView_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadAlertsAsync();
    }

    private async Task LoadAlertsAsync()
    {
        try
        {
            var alerts = await _alertService.GetAlertsAsync();
            _alerts.Clear();
            foreach (var alert in alerts)
                _alerts.Add(alert);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطا در بارگذاری اعلان‌ها:\n{ex.Message}", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        await LoadAlertsAsync();
    }
}
