using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Maktab.Application.Abstractions;

namespace Maktab.App.Wpf.Views;

public partial class BulkImportView : UserControl
{
    private readonly IBulkImportService _bulkImportService;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUserService;

    public BulkImportView(
        IBulkImportService bulkImportService,
        IAuditService auditService,
        ICurrentUserService currentUserService)
    {
        _bulkImportService = bulkImportService;
        _auditService = auditService;
        _currentUserService = currentUserService;

        InitializeComponent();
    }

    private void BrowseFileButton_Click(object sender, RoutedEventArgs e)
    {
        var openFileDialog = new OpenFileDialog
        {
            Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
            Title = "انتخاب فایل CSV"
        };

        if (openFileDialog.ShowDialog() == true)
        {
            FilePathTextBlock.Text = openFileDialog.FileName;
            try
            {
                CsvTextBox.Text = File.ReadAllText(openFileDialog.FileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطا در خواندن فایل:\n{ex.Message}", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private async void ImportButton_Click(object sender, RoutedEventArgs e)
    {
        var csvText = CsvTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(csvText))
        {
            MessageBox.Show("لطفاً محتوای CSV را وارد کنید یا فایل انتخاب کنید.", "ورودی خالی", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var result = await _bulkImportService.ImportStudentsFromCsvAsync(csvText);
            SummaryTextBlock.Text = $"{result.SuccessCount} ردیف با موفقیت وارد شد (از {result.TotalRows} ردیف)";
            ErrorsTextBox.Text = result.Errors.Count > 0 ? string.Join(Environment.NewLine, result.Errors) : "هیچ خطایی وجود ندارد.";

            if (result.SuccessCount > 0)
            {
                await LogAuditAsync($"ورود دسته‌جمعی: {result.SuccessCount} شاگرد");
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطا در عملیات ورود:\n{ex.Message}", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
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
            // Audit logging should not break import
        }
    }
}
