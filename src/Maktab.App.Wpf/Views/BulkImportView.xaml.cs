using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Maktab.Application.Abstractions;

namespace Maktab.App.Wpf.Views;

public partial class BulkImportView : UserControl
{
    private readonly IBulkImportService _bulkImportService;
    private readonly IClassSubjectService _classSubjectService;
    private readonly IAcademicYearService _academicYearService;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUserService;

    private string? _studentsFilePath;
    private string? _marksFilePath;
    private string? _attendanceFilePath;

    public BulkImportView(
        IBulkImportService bulkImportService,
        IClassSubjectService classSubjectService,
        IAcademicYearService academicYearService,
        IAuditService auditService,
        ICurrentUserService currentUserService)
    {
        _bulkImportService = bulkImportService;
        _classSubjectService = classSubjectService;
        _academicYearService = academicYearService;
        _auditService = auditService;
        _currentUserService = currentUserService;

        InitializeComponent();
        Loaded += BulkImportView_Loaded;
    }

    private async void BulkImportView_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadFiltersAsync();
    }

    private async Task LoadFiltersAsync()
    {
        try
        {
            var classes = await _classSubjectService.GetClassesAsync();
            MarksClassComboBox.ItemsSource = classes;
            AttendanceClassComboBox.ItemsSource = classes;
            if (classes.Count > 0)
            {
                MarksClassComboBox.SelectedIndex = 0;
                AttendanceClassComboBox.SelectedIndex = 0;
            }

            var years = await _academicYearService.GetAllAcademicYearsAsync();
            MarksYearComboBox.ItemsSource = years;
            AttendanceYearComboBox.ItemsSource = years;
            var active = years.FirstOrDefault(y => y.IsActive);
            MarksYearComboBox.SelectedItem = active ?? years.FirstOrDefault();
            AttendanceYearComboBox.SelectedItem = active ?? years.FirstOrDefault();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطا در بارگذاری فیلترها:\n{ex.Message}", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void MarksClassComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (MarksClassComboBox.SelectedValue is not int classId || classId <= 0) return;
        try
        {
            var subjects = await _classSubjectService.GetSubjectsByClassAsync(classId);
            MarksSubjectComboBox.ItemsSource = subjects;
            if (subjects.Count > 0) MarksSubjectComboBox.SelectedIndex = 0;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطا در بارگذاری مضامین:\n{ex.Message}", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ---------- Students ----------
    private void BrowseStudentsFileButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "CSV Files (*.csv)|*.csv|Excel Files (*.xlsx)|*.xlsx|All Files (*.*)|*.*",
            Title = "انتخاب فایل CSV/Excel"
        };
        if (dialog.ShowDialog() == true)
        {
            _studentsFilePath = dialog.FileName;
            StudentsFilePathTextBlock.Text = dialog.FileName;
            // If CSV, show content in text box; Excel not previewable
            if (dialog.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            {
                StudentsCsvTextBox.Text = File.ReadAllText(dialog.FileName);
            }
            else
            {
                StudentsCsvTextBox.Clear();
            }
        }
    }

    private async void ImportStudentsButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            BulkImportResultDto result;
            if (!string.IsNullOrWhiteSpace(_studentsFilePath))
            {
                result = await _bulkImportService.ImportStudentsFromFileAsync(_studentsFilePath);
            }
            else
            {
                var csv = StudentsCsvTextBox.Text.Trim();
                if (string.IsNullOrWhiteSpace(csv))
                {
                    MessageBox.Show("لطفاً فایل انتخاب کنید یا محتوای CSV را وارد کنید.", "ورودی خالی", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                result = await _bulkImportService.ImportStudentsFromCsvAsync(csv);
            }

            ShowResult(result, StudentsSummaryTextBlock, StudentsErrorsTextBox);
            if (result.SuccessCount > 0) await LogAuditAsync($"ورود دسته‌جمعی شاگردان: {result.SuccessCount}");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطا در عملیات ورود:\n{ex.Message}", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ---------- Marks ----------
    private void BrowseMarksFileButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "CSV Files (*.csv)|*.csv|Excel Files (*.xlsx)|*.xlsx|All Files (*.*)|*.*",
            Title = "انتخاب فایل CSV/Excel"
        };
        if (dialog.ShowDialog() == true)
        {
            _marksFilePath = dialog.FileName;
            if (dialog.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            {
                MarksCsvTextBox.Text = File.ReadAllText(dialog.FileName);
            }
            else
            {
                MarksCsvTextBox.Clear();
            }
        }
    }

    private async void ImportMarksButton_Click(object sender, RoutedEventArgs e)
    {
        if (MarksClassComboBox.SelectedValue is not int classId || classId <= 0)
        {
            MessageBox.Show("لطفاً صنف را انتخاب کنید.", "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (MarksSubjectComboBox.SelectedValue is not int subjectId || subjectId <= 0)
        {
            MessageBox.Show("لطفاً مضمون را انتخاب کنید.", "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (MarksYearComboBox.SelectedValue is not int yearId || yearId <= 0)
        {
            MessageBox.Show("لطفاً سال تعلیمی را انتخاب کنید.", "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            BulkImportResultDto result;
            if (!string.IsNullOrWhiteSpace(_marksFilePath))
            {
                result = await _bulkImportService.ImportMarksFromFileAsync(_marksFilePath, classId, subjectId, yearId);
            }
            else
            {
                var csv = MarksCsvTextBox.Text.Trim();
                if (string.IsNullOrWhiteSpace(csv))
                {
                    MessageBox.Show("لطفاً فایل انتخاب کنید یا محتوای CSV را وارد کنید.", "ورودی خالی", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                result = await _bulkImportService.ImportMarksFromCsvAsync(csv, classId, subjectId, yearId);
            }

            ShowResult(result, MarksSummaryTextBlock, MarksErrorsTextBox);
            if (result.SuccessCount > 0) await LogAuditAsync($"ورود دسته‌جمعی نمرات: {result.SuccessCount}");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطا در ورود نمرات:\n{ex.Message}", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ---------- Attendance ----------
    private void BrowseAttendanceFileButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "CSV Files (*.csv)|*.csv|Excel Files (*.xlsx)|*.xlsx|All Files (*.*)|*.*",
            Title = "انتخاب فایل CSV/Excel"
        };
        if (dialog.ShowDialog() == true)
        {
            _attendanceFilePath = dialog.FileName;
            if (dialog.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            {
                AttendanceCsvTextBox.Text = File.ReadAllText(dialog.FileName);
            }
            else
            {
                AttendanceCsvTextBox.Clear();
            }
        }
    }

    private async void ImportAttendanceButton_Click(object sender, RoutedEventArgs e)
    {
        if (AttendanceClassComboBox.SelectedValue is not int classId || classId <= 0)
        {
            MessageBox.Show("لطفاً صنف را انتخاب کنید.", "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (AttendanceYearComboBox.SelectedValue is not int yearId || yearId <= 0)
        {
            MessageBox.Show("لطفاً سال تعلیمی را انتخاب کنید.", "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            BulkImportResultDto result;
            if (!string.IsNullOrWhiteSpace(_attendanceFilePath))
            {
                result = await _bulkImportService.ImportAttendanceFromFileAsync(_attendanceFilePath, classId, yearId);
            }
            else
            {
                var csv = AttendanceCsvTextBox.Text.Trim();
                if (string.IsNullOrWhiteSpace(csv))
                {
                    MessageBox.Show("لطفاً فایل انتخاب کنید یا محتوای CSV را وارد کنید.", "ورودی خالی", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                result = await _bulkImportService.ImportAttendanceFromCsvAsync(csv, classId, yearId);
            }

            ShowResult(result, AttendanceSummaryTextBlock, AttendanceErrorsTextBox);
            if (result.SuccessCount > 0) await LogAuditAsync($"ورود دسته‌جمعی حاضری: {result.SuccessCount}");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطا در ورود حاضری:\n{ex.Message}", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ShowResult(BulkImportResultDto result, TextBlock summary, TextBox errors)
    {
        summary.Text = $"{result.SuccessCount} ردیف با موفقیت وارد شد (از {result.TotalRows} ردیف)";
        errors.Text = result.Errors.Count > 0 ? string.Join(Environment.NewLine, result.Errors) : "هیچ خطایی وجود ندارد.";
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
