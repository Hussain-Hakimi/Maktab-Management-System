using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using Maktab.Application.Abstractions;
using Maktab.Infrastructure.Reports;

namespace Maktab.App.Wpf.Views;

public partial class BulkImportView
{
    private IBulkImportPreviewService BulkImportPreviewService
        => _bulkImportPreviewService ??= new BulkImportPreviewService(
            App.Services?.GetRequiredService<IStudentService>() ?? throw new InvalidOperationException("Application services are not initialized."),
            _classSubjectService,
            new ExcelReader());

    private static void RegisterPreviewHandlers()
        => EventManager.RegisterClassHandler(typeof(Button), Button.ClickEvent, new RoutedEventHandler(OnBulkImportButtonClick));

    static BulkImportView() => RegisterPreviewHandlers();

    private static void OnBulkImportButtonClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Name is not (
            "ImportStudentsButton" or "ImportMarksButton" or "ImportAttendanceButton" or "ImportMultiMarksButton")) return;

        var view = FindView(button);
        if (view is null) return;

        e.Handled = true;
        _ = view.PreviewAndImportAsync(button.Name);
    }

    private static BulkImportView? FindView(DependencyObject current)
    {
        while (current is not null)
        {
            if (current is BulkImportView view) return view;
            current = current is FrameworkElement element ? element.Parent : null;
        }
        return null;
    }

    private async Task PreviewAndImportAsync(string buttonName)
    {
        try
        {
            switch (buttonName)
            {
                case "ImportStudentsButton": await PreviewStudentsAndImportAsync(); break;
                case "ImportMarksButton": await PreviewMarksAndImportAsync(); break;
                case "ImportAttendanceButton": await PreviewAttendanceAndImportAsync(); break;
                case "ImportMultiMarksButton": await PreviewMultiMarksAndImportAsync(); break;
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            MessageBox.Show($"خطا در بررسی فایل:\n{ex.Message}", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task PreviewStudentsAndImportAsync()
    {
        var preview = !string.IsNullOrWhiteSpace(_studentsFilePath)
            ? await BulkImportPreviewService.PreviewStudentsFromFileAsync(_studentsFilePath)
            : await BulkImportPreviewService.PreviewStudentsFromCsvAsync(StudentsCsvTextBox.Text.Trim());
        ShowPreviewResult(preview, StudentsSummaryTextBlock, StudentsErrorsTextBox, "شاگردان");
        if (!preview.CanImport) { OfferErrorReport(preview.Errors, "گزارش_خطا_شاگردان"); return; }
        if (!ConfirmImport(preview, "شاگرد")) return;

        var result = !string.IsNullOrWhiteSpace(_studentsFilePath)
            ? await _bulkImportService.ImportStudentsFromFileAsync(_studentsFilePath)
            : await _bulkImportService.ImportStudentsFromCsvAsync(StudentsCsvTextBox.Text.Trim());
        ShowResult(result, StudentsSummaryTextBlock, StudentsErrorsTextBox);
        if (result.Errors.Count > 0) OfferErrorReport(result.Errors, "گزارش_خطا_شاگردان");
        if (result.SuccessCount > 0) await LogAuditAsync($"ورود دسته‌جمعی شاگردان: {result.SuccessCount}");
    }

    private async Task PreviewMarksAndImportAsync()
    {
        if (MarksClassComboBox.SelectedValue is not int classId || classId <= 0 ||
            MarksSubjectComboBox.SelectedValue is not int subjectId || subjectId <= 0 ||
            MarksYearComboBox.SelectedValue is not int yearId || yearId <= 0)
        { MessageBox.Show("لطفاً صنف، مضمون و سال تعلیمی را انتخاب کنید.", "اطلاعات ناقص", MessageBoxButton.OK, MessageBoxImage.Warning); return; }

        var preview = !string.IsNullOrWhiteSpace(_marksFilePath)
            ? await BulkImportPreviewService.PreviewMarksFromFileAsync(_marksFilePath, classId, subjectId, yearId)
            : await BulkImportPreviewService.PreviewMarksFromCsvAsync(MarksCsvTextBox.Text.Trim(), classId, subjectId, yearId);
        ShowPreviewResult(preview, MarksSummaryTextBlock, MarksErrorsTextBox, "نمرات");
        if (!preview.CanImport) { OfferErrorReport(preview.Errors, "گزارش_خطا_نمرات"); return; }
        if (!ConfirmImport(preview, "ردیف نمرات")) return;

        var result = !string.IsNullOrWhiteSpace(_marksFilePath)
            ? await _bulkImportService.ImportMarksFromFileAsync(_marksFilePath, classId, subjectId, yearId)
            : await _bulkImportService.ImportMarksFromCsvAsync(MarksCsvTextBox.Text.Trim(), classId, subjectId, yearId);
        ShowResult(result, MarksSummaryTextBlock, MarksErrorsTextBox);
        if (result.Errors.Count > 0) OfferErrorReport(result.Errors, "گزارش_خطا_نمرات");
        if (result.SuccessCount > 0) await LogAuditAsync($"ورود دسته‌جمعی نمرات: {result.SuccessCount}");
    }

    private async Task PreviewAttendanceAndImportAsync()
    {
        if (AttendanceClassComboBox.SelectedValue is not int classId || classId <= 0 ||
            AttendanceYearComboBox.SelectedValue is not int yearId || yearId <= 0)
        { MessageBox.Show("لطفاً صنف و سال تعلیمی را انتخاب کنید.", "اطلاعات ناقص", MessageBoxButton.OK, MessageBoxImage.Warning); return; }

        var preview = !string.IsNullOrWhiteSpace(_attendanceFilePath)
            ? await BulkImportPreviewService.PreviewAttendanceFromFileAsync(_attendanceFilePath, classId, yearId)
            : await BulkImportPreviewService.PreviewAttendanceFromCsvAsync(AttendanceCsvTextBox.Text.Trim(), classId, yearId);
        ShowPreviewResult(preview, AttendanceSummaryTextBlock, AttendanceErrorsTextBox, "حاضری");
        if (!preview.CanImport) { OfferErrorReport(preview.Errors, "گزارش_خطا_حاضری"); return; }
        if (!ConfirmImport(preview, "ردیف حاضری")) return;

        var result = !string.IsNullOrWhiteSpace(_attendanceFilePath)
            ? await _bulkImportService.ImportAttendanceFromFileAsync(_attendanceFilePath, classId, yearId)
            : await _bulkImportService.ImportAttendanceFromCsvAsync(AttendanceCsvTextBox.Text.Trim(), classId, yearId);
        ShowResult(result, AttendanceSummaryTextBlock, AttendanceErrorsTextBox);
        if (result.Errors.Count > 0) OfferErrorReport(result.Errors, "گزارش_خطا_حاضری");
        if (result.SuccessCount > 0) await LogAuditAsync($"ورود دسته‌جمعی حاضری: {result.SuccessCount}");
    }

    private async Task PreviewMultiMarksAndImportAsync()
    {
        if (MultiMarksClassComboBox.SelectedValue is not int classId || classId <= 0 ||
            MultiMarksYearComboBox.SelectedValue is not int yearId || yearId <= 0 ||
            string.IsNullOrWhiteSpace(_multiMarksFilePath))
        { MessageBox.Show("لطفاً صنف، سال تعلیمی و فایل Excel را انتخاب کنید.", "اطلاعات ناقص", MessageBoxButton.OK, MessageBoxImage.Warning); return; }

        var preview = await BulkImportPreviewService.PreviewMultiSubjectMarksFromFileAsync(_multiMarksFilePath, classId, yearId);
        ShowPreviewResult(preview, MultiMarksSummaryTextBlock, MultiMarksErrorsTextBox, "نمرات چند مضمون");
        if (!preview.CanImport) { OfferErrorReport(preview.Errors, "گزارش_خطا_نمرات_چند_مضمون"); return; }
        if (!ConfirmImport(preview, "ردیف نمرات چند مضمون")) return;

        var result = await _bulkImportService.ImportMultiSubjectMarksFromFileAsync(_multiMarksFilePath, classId, yearId);
        ShowResult(result, MultiMarksSummaryTextBlock, MultiMarksErrorsTextBox);
        if (result.Errors.Count > 0) OfferErrorReport(result.Errors, "گزارش_خطا_نمرات_چند_مضمون");
        if (result.SuccessCount > 0) await LogAuditAsync($"ورود نمرات چند مضمون: {result.SuccessCount} ردیف");
    }

    private static bool ConfirmImport(BulkImportPreviewResult preview, string itemName)
        => MessageBox.Show(
            $"بررسی فایل تکمیل شد.\n\nتعداد ردیف‌های معتبر: {preview.ValidRows}\nتعداد ردیف‌ها: {preview.TotalRows}\n\nآیا می‌خواهید {itemName} را وارد سیستم کنید؟\nاین عملیات اطلاعات فایل را در دیتابیس ذخیره خواهد کرد.",
            "تأیید ورود", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;

    private static void ShowPreviewResult(BulkImportPreviewResult result, TextBlock summary, TextBox errors, string name)
    {
        summary.Text = $"پیش‌نمایش {name}: {result.ValidRows} ردیف معتبر از {result.TotalRows} ردیف";
        errors.Text = result.Errors.Count == 0 ? "پیش‌نمایش موفق بود؛ خطایی یافت نشد." : string.Join(Environment.NewLine, result.Errors);
    }

    private static void OfferErrorReport(IReadOnlyList<string> errors, string defaultName)
    {
        if (errors.Count == 0) return;
        var dialog = new SaveFileDialog { FileName = defaultName, DefaultExt = ".txt", Filter = "Text Files (*.txt)|*.txt" };
        if (dialog.ShowDialog() != true) return;
        File.WriteAllLines(dialog.FileName, errors);
        MessageBox.Show($"گزارش خطا ذخیره شد:\n{dialog.FileName}", "گزارش خطا", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private IBulkImportPreviewService? _bulkImportPreviewService;
}
