using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Maktab.Application.Abstractions;

namespace Maktab.App.Wpf.Views;

public partial class AuditLogView : UserControl
{
    private readonly IAuditService _auditService;
    private readonly ObservableCollection<AuditLogDto> _logs = [];

    public AuditLogView(IAuditService auditService)
    {
        _auditService = auditService;
        InitializeComponent();

        AuditDataGrid.ItemsSource = _logs;
        Loaded += AuditLogView_Loaded;
    }

    private async void AuditLogView_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadLogsAsync();
    }

    private async Task LoadLogsAsync()
    {
        try
        {
            var logs = await _auditService.GetRecentLogsAsync(100);
            _logs.Clear();
            foreach (var log in logs)
                _logs.Add(log);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطا در دریافت گزارش وقایع:\n{ex.Message}", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        await LoadLogsAsync();
    }
}
