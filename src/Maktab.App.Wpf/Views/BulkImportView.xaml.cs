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
            MultiMarksClassComboBox.ItemsSource = classes;
            if (classes.Count > 0)
            {
                MarksClassComboBox.SelectedIndex = 0;
                AttendanceClassComboBox.SelectedIndex = 0;
                MultiMarksClassComboBox.SelectedIndex = 0;
            }

            var years = await _academicYearService.GetAllAcademicYearsAsync();
            MarksYearComboBox.ItemsSource = years;
            AttendanceYearComboBox.ItemsSource = years;
            MultiMarksYearComboBox.ItemsSource = years;
            var active = years.FirstOrDefault(y => y.IsActive);
            MarksYearComboBox.SelectedItem = active ?? years.FirstOrDefault();
            AttendanceYearComboBox.SelectedItem = active ?? years.FirstOrDefault();
            MultiMarksYearComboBox.SelectedItem = active ?? years.FirstOrDefault();
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

    // ==================== Download Template Handlers ====================

    private void DownloadStudentsTemplate_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            FileName = "فارمت_شاگردان",
            DefaultExt = ".xlsx",
            Filter = "Excel Files (*.xlsx)|*.xlsx"
        };
        if (dialog.ShowDialog() != true) return;

        try
        {
            using var workbook = new ClosedXML.Excel.XLWorkbook();
            var sheet = workbook.Worksheets.Add("Students");

            // Header row
            sheet.Cell(1, 1).Value = "FirstName";
            sheet.Cell(1, 2).Value = "LastName";
            sheet.Cell(1, 3).Value = "FatherName";
            sheet.Cell(1, 4).Value = "RollNumber";
            sheet.Cell(1, 5).Value = "ClassName";

            // Style header
            var headerRange = sheet.Range("A1:E1");
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#334155");
            headerRange.Style.Font.FontColor = ClosedXML.Excel.XLColor.White;

            // Sample rows
            sheet.Cell(2, 1).Value = "احمد";
            sheet.Cell(2, 2).Value = "محمدی";
            sheet.Cell(2, 3).Value = "محمد";
            sheet.Cell(2, 4).Value = "101";
            sheet.Cell(2, 5).Value = "صنف اول";

            sheet.Cell(3, 1).Value = "فاطمه";
            sheet.Cell(3, 2).Value = "احمدی";
            sheet.Cell(3, 3).Value = "احمد";
            sheet.Cell(3, 4).Value = "102";
            sheet.Cell(3, 5).Value = "صنف اول";

            // Notes sheet
            var notesSheet = workbook.Worksheets.Add("راهنما");
            notesSheet.Cell(1, 1).Value = "راهنمای تکمیل فارمت شاگردان";
            notesSheet.Cell(1, 1).Style.Font.Bold = true;
            notesSheet.Cell(1, 1).Style.Font.FontSize = 14;
            notesSheet.Cell(3, 1).Value = "FirstName";
            notesSheet.Cell(3, 2).Value = "نام شاگرد (اجباری)";
            notesSheet.Cell(4, 1).Value = "LastName";
            notesSheet.Cell(4, 2).Value = "تخلص شاگرد (اجباری)";
            notesSheet.Cell(5, 1).Value = "FatherName";
            notesSheet.Cell(5, 2).Value = "نام پدر (اجباری)";
            notesSheet.Cell(6, 1).Value = "RollNumber";
            notesSheet.Cell(6, 2).Value = "شماره اساس (اجباری، باید یونیک باشد)";
            notesSheet.Cell(7, 1).Value = "ClassName";
            notesSheet.Cell(7, 2).Value = "نام صنف دقیقاً مطابق سیستم (مثلاً: صنف اول)";
            notesSheet.Columns().AdjustToContents();

            sheet.Columns().AdjustToContents();
            workbook.SaveAs(dialog.FileName);
            MessageBox.Show($"✅ فارمت نمونه در مسیر زیر ذخیره شد:\n{dialog.FileName}", "دانلود موفق", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطا در ساخت فارمت:\n{ex.Message}", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void DownloadMarksTemplate_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            FileName = "فارمت_نمرات",
            DefaultExt = ".xlsx",
            Filter = "Excel Files (*.xlsx)|*.xlsx"
        };
        if (dialog.ShowDialog() != true) return;

        try
        {
            using var workbook = new ClosedXML.Excel.XLWorkbook();
            var sheet = workbook.Worksheets.Add("Marks");

            sheet.Cell(1, 1).Value = "RollNumber";
            sheet.Cell(1, 2).Value = "MidtermScore";
            sheet.Cell(1, 3).Value = "FinalScore";

            var headerRange = sheet.Range("A1:C1");
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#334155");
            headerRange.Style.Font.FontColor = ClosedXML.Excel.XLColor.White;

            sheet.Cell(2, 1).Value = "101";
            sheet.Cell(2, 2).Value = 18.5;
            sheet.Cell(2, 3).Value = 85;

            sheet.Cell(3, 1).Value = "102";
            sheet.Cell(3, 2).Value = 16;
            sheet.Cell(3, 3).Value = 78;

            sheet.Columns().AdjustToContents();
            workbook.SaveAs(dialog.FileName);
            MessageBox.Show($"✅ فارمت نمونه ذخیره شد:\n{dialog.FileName}", "دانلود موفق", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطا در ساخت فارمت:\n{ex.Message}", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void DownloadAttendanceTemplate_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            FileName = "فارمت_حاضری",
            DefaultExt = ".xlsx",
            Filter = "Excel Files (*.xlsx)|*.xlsx"
        };
        if (dialog.ShowDialog() != true) return;

        try
        {
            using var workbook = new ClosedXML.Excel.XLWorkbook();
            var sheet = workbook.Worksheets.Add("Attendance");

            sheet.Cell(1, 1).Value = "RollNumber";
            sheet.Cell(1, 2).Value = "Date";
            sheet.Cell(1, 3).Value = "Status";

            var headerRange = sheet.Range("A1:C1");
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#334155");
            headerRange.Style.Font.FontColor = ClosedXML.Excel.XLColor.White;

            sheet.Cell(2, 1).Value = "101";
            sheet.Cell(2, 2).Value = DateTime.Today.ToString("yyyy-MM-dd");
            sheet.Cell(2, 3).Value = "Present";

            sheet.Cell(3, 1).Value = "102";
            sheet.Cell(3, 2).Value = DateTime.Today.ToString("yyyy-MM-dd");
            sheet.Cell(3, 3).Value = "Absent";

            // Notes
            var notes = workbook.Worksheets.Add("راهنما");
            notes.Cell(1, 1).Value = "مقادیر معتبر برای ستون Status:";
            notes.Cell(1, 1).Style.Font.Bold = true;
            notes.Cell(2, 1).Value = "Present";
            notes.Cell(2, 2).Value = "حاضر";
            notes.Cell(3, 1).Value = "Absent";
            notes.Cell(3, 2).Value = "غایب";
            notes.Cell(4, 1).Value = "Late";
            notes.Cell(4, 2).Value = "دیر آمده";
            notes.Cell(6, 1).Value = "فارمت تاریخ: YYYY-MM-DD (مثلاً: 2024-03-15)";
            notes.Columns().AdjustToContents();

            sheet.Columns().AdjustToContents();
            workbook.SaveAs(dialog.FileName);
            MessageBox.Show($"✅ فارمت نمونه ذخیره شد:\n{dialog.FileName}", "دانلود موفق", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطا در ساخت فارمت:\n{ex.Message}", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ==================== Multi-Subject Marks Handlers ====================

    private string? _multiMarksFilePath;

    private async void MultiMarksClassComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Nothing special needed here — class selection drives the template download
        await Task.CompletedTask;
    }

    private async void DownloadMultiMarksTemplate_Click(object sender, RoutedEventArgs e)
    {
        if (MultiMarksClassComboBox.SelectedValue is not int classId || classId <= 0)
        {
            MessageBox.Show("لطفاً ابتدا صنف را انتخاب کنید تا ستون‌های مضامین آن در فارمت ساخته شوند.", "صنف انتخاب نشده", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var subjects = await _classSubjectService.GetSubjectsByClassAsync(classId);
            if (subjects.Count == 0)
            {
                MessageBox.Show("این صنف هیچ مضمونی ندارد. ابتدا مضامین صنف را اضافه کنید.", "مضمون یافت نشد", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                FileName = "فارمت_نمرات_چند_مضمون",
                DefaultExt = ".xlsx",
                Filter = "Excel Files (*.xlsx)|*.xlsx"
            };
            if (dialog.ShowDialog() != true) return;

            using var workbook = new ClosedXML.Excel.XLWorkbook();
            var sheet = workbook.Worksheets.Add("MultiSubjectMarks");

            // Build header row: RollNumber + SubjectName_Midterm + SubjectName_Final for each subject
            sheet.Cell(1, 1).Value = "RollNumber";
            int col = 2;
            foreach (var subject in subjects)
            {
                sheet.Cell(1, col).Value = $"{subject.SubjectName}_Midterm";
                sheet.Cell(1, col + 1).Value = $"{subject.SubjectName}_Final";
                col += 2;
            }

            var headerRange = sheet.Range(1, 1, 1, col - 1);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#334155");
            headerRange.Style.Font.FontColor = ClosedXML.Excel.XLColor.White;

            // Sample row
            sheet.Cell(2, 1).Value = "101";
            col = 2;
            foreach (var _ in subjects)
            {
                sheet.Cell(2, col).Value = 0;
                sheet.Cell(2, col + 1).Value = 0;
                col += 2;
            }

            sheet.Columns().AdjustToContents();
            workbook.SaveAs(dialog.FileName);
            MessageBox.Show($"✅ فارمت نمونه با {subjects.Count} مضمون ذخیره شد:\n{dialog.FileName}", "دانلود موفق", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطا در ساخت فارمت:\n{ex.Message}", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BrowseMultiMarksFileButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Excel Files (*.xlsx)|*.xlsx",
            Title = "انتخاب فایل Excel نمرات چند مضمون"
        };
        if (dialog.ShowDialog() == true)
        {
            _multiMarksFilePath = dialog.FileName;
            MultiMarksFilePathTextBlock.Text = $"📄 فایل انتخاب شد: {dialog.FileName}";
        }
    }

    private async void ImportMultiMarksButton_Click(object sender, RoutedEventArgs e)
    {
        if (MultiMarksClassComboBox.SelectedValue is not int classId || classId <= 0)
        {
            MessageBox.Show("لطفاً صنف را انتخاب کنید.", "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (MultiMarksYearComboBox.SelectedValue is not int yearId || yearId <= 0)
        {
            MessageBox.Show("لطفاً سال تعلیمی را انتخاب کنید.", "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (string.IsNullOrWhiteSpace(_multiMarksFilePath))
        {
            MessageBox.Show("لطفاً ابتدا فایل Excel را انتخاب کنید.", "فایل انتخاب نشده", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var result = await _bulkImportService.ImportMultiSubjectMarksFromFileAsync(_multiMarksFilePath, classId, yearId);
            ShowResult(result, MultiMarksSummaryTextBlock, MultiMarksErrorsTextBox);
            if (result.SuccessCount > 0) await LogAuditAsync($"ورود نمرات چند مضمون: {result.SuccessCount} ردیف");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطا در ورود نمرات:\n{ex.Message}", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
