using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Maktab.Application.Abstractions;
using Maktab.Application.Services;
using Maktab.Domain.Entities;
using Maktab.Domain.Enums;
using Maktab.Infrastructure.Persistence;

namespace Maktab.App.Wpf.Views;

public sealed class StudentComboItem
{
    public int StudentId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
}

public sealed class PreviewMarkItem
{
    public string SubjectName { get; set; } = string.Empty;
    public decimal MidtermScore { get; set; }
    public decimal FinalScore { get; set; }
    public decimal TotalScore { get; set; }
    public string IsPassText { get; set; } = string.Empty;
}

public sealed class TemplateItem
{
    public string Name { get; set; } = string.Empty;
    public ReportCardTemplateType Value { get; set; }
}

public partial class ReportCardsView : UserControl
{
    private readonly IReportCardService _reportCardService;
    private readonly IClassSubjectService _classSubjectService;
    private readonly IStudentService _studentService;
    private readonly AppFolders _appFolders;
    private readonly ITeacherAssignmentService _teacherAssignmentService;
    private readonly ICurrentUserService _currentUserService;

    private readonly List<SchoolClass> _classes = [];
    private readonly List<Student> _classStudents = [];
    private readonly ObservableCollection<PreviewMarkItem> _previewMarks = [];
    private string? _lastGeneratedPdfPath;
    private bool _isCurrentUserGuardianOfSelectedClass;
    private bool _isAdmin;

    public ReportCardsView(
        IReportCardService reportCardService,
        IClassSubjectService classSubjectService,
        IStudentService studentService,
        AppFolders appFolders,
        ITeacherAssignmentService teacherAssignmentService,
        ICurrentUserService currentUserService)
    {
        _reportCardService = reportCardService;
        _classSubjectService = classSubjectService;
        _studentService = studentService;
        _appFolders = appFolders;
        _teacherAssignmentService = teacherAssignmentService;
        _currentUserService = currentUserService;

        InitializeComponent();

        AcademicYearTextBox.Text = AcademicYearProvider.GetCurrentAcademicYear();
        PreviewMarksDataGrid.ItemsSource = _previewMarks;
        Loaded += ReportCardsView_Loaded;
    }

    private async void ReportCardsView_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadClassesAsync();
        ApplyPermissionUi();
        UpdatePreviewColumns(GetSelectedReportType());
    }

    public async Task InitializeDataAsync()
    {
        await LoadClassesAsync();
        ApplyPermissionUi();
        UpdatePreviewColumns(GetSelectedReportType());
    }

    private ReportCardType GetSelectedReportType()
    {
        var selected = ReportTypeComboBox.SelectedItem as ComboBoxItem;
        return selected?.Tag?.ToString() == "Midterm" ? ReportCardType.Midterm : ReportCardType.Annual;
    }

    private void UpdatePreviewColumns(ReportCardType reportType)
    {
        PreviewMarksDataGrid.Columns.Clear();

        if (reportType == ReportCardType.Midterm)
        {
            PreviewMarksDataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "نام مضمون",
                Binding = new System.Windows.Data.Binding("SubjectName"),
                Width = new DataGridLength(1, DataGridLengthUnitType.Star)
            });
            PreviewMarksDataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "نمره چهارماهه (۴۰)",
                Binding = new System.Windows.Data.Binding("MidtermScore") { StringFormat = "{}{0:0.##}" },
                Width = 120
            });
            PreviewMarksDataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "فیصدی",
                Binding = new System.Windows.Data.Binding("Percentage") { StringFormat = "{}{0:0.##}%" },
                Width = 80
            });
        }
        else
        {
            PreviewMarksDataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "نام مضمون",
                Binding = new System.Windows.Data.Binding("SubjectName"),
                Width = new DataGridLength(1, DataGridLengthUnitType.Star)
            });
            PreviewMarksDataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "چهارونیم‌ماهه (۴۰)",
                Binding = new System.Windows.Data.Binding("MidtermScore") { StringFormat = "{}{0:0.##}" },
                Width = 120
            });
            PreviewMarksDataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "سالانه (۶۰)",
                Binding = new System.Windows.Data.Binding("FinalScore") { StringFormat = "{}{0:0.##}" },
                Width = 100
            });
            PreviewMarksDataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "مجموع (۱۰۰)",
                Binding = new System.Windows.Data.Binding("TotalScore") { StringFormat = "{}{0:0.##}" },
                Width = 100,
                FontWeight = FontWeights.Bold
            });
            PreviewMarksDataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "نتیجه",
                Binding = new System.Windows.Data.Binding("IsPassText"),
                Width = 80,
                FontWeight = FontWeights.Bold
            });
        }
    }

    private async Task LoadClassesAsync()
    {
        try
        {
            var allClasses = await _classSubjectService.GetClassesAsync();
            var currentUser = _currentUserService.CurrentUser;

            _isAdmin = currentUser?.Role == UserRole.Admin;

            if (_isAdmin)
            {
                _classes.Clear();
                _classes.AddRange(allClasses);
            }
            else if (currentUser is not null && currentUser.Role == UserRole.Teacher)
            {
                var guardianships = await _teacherAssignmentService.GetClassGuardiansAsync(currentUser.UserId);
                var guardianClassIds = guardianships.Select(g => g.ClassId).ToHashSet();
                _classes.Clear();
                _classes.AddRange(allClasses.Where(c => guardianClassIds.Contains(c.ClassId)));
            }
            else
            {
                _classes.Clear();
            }

            ClassComboBox.ItemsSource = _classes.ToList();
            if (_classes.Count > 0)
            {
                ClassComboBox.SelectedIndex = 0;
            }
            else
            {
                StudentComboBox.ItemsSource = null;
                ClearPreview();
                StatusTextBlock.Text = "شما به هیچ صنفی دسترسی کارنامه ندارید.";
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطا در دریافت صنف‌ها:\n{ex.Message}", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void ClassComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ClassComboBox.SelectedValue is int classId && classId > 0)
        {
            await LoadStudentsForClassAsync(classId);
            await UpdateGuardianStatusAsync(classId);
        }
        else
        {
            _classStudents.Clear();
            StudentComboBox.ItemsSource = null;
            ClearPreview();
            _isCurrentUserGuardianOfSelectedClass = false;
        }

        ApplyPermissionUi();
    }

    private async Task UpdateGuardianStatusAsync(int classId)
    {
        var currentUser = _currentUserService.CurrentUser;
        if (currentUser?.Role == UserRole.Admin)
        {
            _isCurrentUserGuardianOfSelectedClass = true;
            return;
        }

        if (currentUser is not null && currentUser.Role == UserRole.Teacher)
        {
            _isCurrentUserGuardianOfSelectedClass = await _teacherAssignmentService.IsClassGuardianAsync(
                currentUser.UserId,
                classId);
        }
        else
        {
            _isCurrentUserGuardianOfSelectedClass = false;
        }
    }

    private void ApplyPermissionUi()
    {
        bool canGenerate = _isAdmin || _isCurrentUserGuardianOfSelectedClass;
        GenerateClassPdfButton.IsEnabled = canGenerate;
        GenerateSinglePdfButton.IsEnabled = canGenerate;
    }

    private async Task LoadStudentsForClassAsync(int classId)
    {
        try
        {
            var students = await _studentService.GetStudentsByClassAsync(classId);
            _classStudents.Clear();
            _classStudents.AddRange(students);

            var comboItems = _classStudents.Select(s => new StudentComboItem
            {
                StudentId = s.StudentId,
                DisplayName = $"{s.FirstName} {s.LastName} (اساس: {s.RollNumber})"
            }).ToList();

            StudentComboBox.ItemsSource = comboItems;
            if (comboItems.Count > 0)
            {
                StudentComboBox.SelectedIndex = 0;
            }
            else
            {
                ClearPreview();
                StatusTextBlock.Text = "برای این صنف هیچ شاگردی ثبت نشده است.";
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطا در بارگذاری شاگردان:\n{ex.Message}", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void StudentComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        await LoadStudentReportPreviewAsync();
    }

    private async void ReportTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdatePreviewColumns(GetSelectedReportType());
        await LoadStudentReportPreviewAsync();
    }

    private async Task LoadStudentReportPreviewAsync()
    {
        if (StudentComboBox.SelectedValue is not int studentId || studentId <= 0)
        {
            ClearPreview();
            return;
        }

        try
        {
            var year = GetAcademicYear();
            var reportType = GetSelectedReportType();
            var data = await _reportCardService.GetStudentReportCardDataAsync(studentId, year);
            data.ReportType = reportType;

            StudentNameTextBlock.Text = $"نام شاگرد: {data.FirstName} {data.LastName}";
            FatherNameTextBlock.Text = $"نام پدر: {data.FatherName}";
            ClassNameTextBlock.Text = $"صنف: {data.ClassName}";
            RollNumberTextBlock.Text = $"شماره اساس: {data.RollNumber}";

            if (reportType == ReportCardType.Midterm)
            {
                // For midterm, only show midterm scores and percentage
                TotalScoreTextBlock.Text = $"مجموع نمرات: {data.SubjectMarks.Sum(m => m.MidtermScore)} از {data.SubjectMarks.Count * 40}";
                AveragePercentageTextBlock.Text = $"اوسط فیصدی: {data.AveragePercentage:0.##}%";
                PassedFailedTextBlock.Text = $"کامیاب: {data.PassedSubjectsCount} | ناکام: {data.FailedSubjectsCount}";
            }
            else
            {
                TotalScoreTextBlock.Text = $"مجموع نمرات: {data.TotalObtainedScore:0.##} از {data.TotalMaxScore:0.##}";
                AveragePercentageTextBlock.Text = $"اوسط فیصدی: {data.AveragePercentage:0.##}%";
                PassedFailedTextBlock.Text = $"کامیاب: {data.PassedSubjectsCount} | ناکام: {data.FailedSubjectsCount}";
            }

            // Promotion badge (only for Annual)
            if (reportType == ReportCardType.Annual)
            {
                switch (data.PromotionOutcome)
                {
                    case PromotionOutcome.Promoted:
                        PromotionBadgeBorder.Background = new SolidColorBrush(Color.FromRgb(240, 253, 244));
                        PromotionBadgeBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(34, 197, 94));
                        PromotionStatusTitleTextBlock.Text = "وضعیت ارتقاء: ✅ ارتقاء به صنف بالا (کامیاب)";
                        PromotionStatusTitleTextBlock.Foreground = new SolidColorBrush(Color.FromRgb(22, 163, 74));
                        break;
                    case PromotionOutcome.Conditional:
                        PromotionBadgeBorder.Background = new SolidColorBrush(Color.FromRgb(254, 252, 232));
                        PromotionBadgeBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(234, 179, 8));
                        PromotionStatusTitleTextBlock.Text = "وضعیت ارتقاء: 🟡 مشروط (نیاز به بازنگری)";
                        PromotionStatusTitleTextBlock.Foreground = new SolidColorBrush(Color.FromRgb(202, 138, 4));
                        break;
                    default:
                        PromotionBadgeBorder.Background = new SolidColorBrush(Color.FromRgb(254, 242, 242));
                        PromotionBadgeBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(239, 68, 68));
                        PromotionStatusTitleTextBlock.Text = "وضعیت ارتقاء: ❌ تکرار صنف (ناکام)";
                        PromotionStatusTitleTextBlock.Foreground = new SolidColorBrush(Color.FromRgb(220, 38, 38));
                        break;
                }
                PromotionReasonTextBlock.Text = data.FailureReason != null ? $"علت: {data.FailureReason}" : string.Empty;
            }
            else
            {
                PromotionBadgeBorder.Background = new SolidColorBrush(Color.FromRgb(241, 245, 249));
                PromotionBadgeBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(203, 213, 225));
                PromotionStatusTitleTextBlock.Text = "وضعیت ارتقاء: —";
                PromotionReasonTextBlock.Text = string.Empty;
            }

            _previewMarks.Clear();
            foreach (var m in data.SubjectMarks)
            {
                var item = new PreviewMarkItem
                {
                    SubjectName = m.SubjectName,
                    MidtermScore = m.MidtermScore,
                    FinalScore = m.FinalScore,
                    TotalScore = m.TotalScore,
                    IsPassText = m.IsPass ? "کامیاب" : "ناکام"
                };
                _previewMarks.Add(item);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطا در دریافت اطلاعات کارنامه:\n{ex.Message}", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private string GetAcademicYear()
    {
        var year = AcademicYearTextBox.Text?.Trim();
        return string.IsNullOrWhiteSpace(year) ? AcademicYearProvider.GetCurrentAcademicYear() : year;
    }

    private void ClearPreview()
    {
        StudentNameTextBlock.Text = "نام شاگرد: -";
        FatherNameTextBlock.Text = "نام پدر: -";
        ClassNameTextBlock.Text = "صنف: -";
        RollNumberTextBlock.Text = "شماره اساس: -";
        TotalScoreTextBlock.Text = "مجموع نمرات: ۰ / ۰";
        AveragePercentageTextBlock.Text = "اوسط فیصدی: ۰%";
        PassedFailedTextBlock.Text = "کامیاب: ۰ | ناکام: ۰";
        PromotionStatusTitleTextBlock.Text = "وضعیت ارتقاء: نامشخص";
        PromotionReasonTextBlock.Text = string.Empty;
        _previewMarks.Clear();
    }

    private bool HasReportCardPermission()
    {
        return _isAdmin || _isCurrentUserGuardianOfSelectedClass;
    }

    private async void GenerateSinglePdfButton_Click(object sender, RoutedEventArgs e)
    {
        if (!HasReportCardPermission())
        {
            MessageBox.Show("شما اجازه تولید کارنامه این صنف را ندارید.", "دسترسی محدود", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (StudentComboBox.SelectedValue is not int studentId || studentId <= 0)
        {
            MessageBox.Show("لطفاً یک شاگرد را انتخاب نمایید.", "شاگرد انتخاب نشده", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            var year = GetAcademicYear();
            var reportType = GetSelectedReportType();
            var filePath = await _reportCardService.GenerateStudentReportCardPdfAsync(studentId, year, _appFolders.Reports, reportType);

            _lastGeneratedPdfPath = filePath;
            OpenGeneratedPdfButton.IsEnabled = true;
            StatusTextBlock.Text = $"✅ کارنامه PDF با موفقیت صادر شد: {Path.GetFileName(filePath)}";

            var openNow = MessageBox.Show($"کارنامه با موفقیت ایجاد شد.\nمحل فایل:\n{filePath}\n\nآیا می‌خواهید فایل باز شود؟", "صدور موفق", MessageBoxButton.YesNo, MessageBoxImage.Information);
            if (openNow == MessageBoxResult.Yes)
            {
                OpenPdf(filePath);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطا در صدور فایل PDF:\n{ex.Message}", "خطا در تولید PDF", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void GenerateClassPdfButton_Click(object sender, RoutedEventArgs e)
    {
        if (!HasReportCardPermission())
        {
            MessageBox.Show("شما اجازه تولید کارنامه کل صنف را ندارید.", "دسترسی محدود", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (ClassComboBox.SelectedValue is not int classId || classId <= 0)
        {
            MessageBox.Show("لطفاً یک صنف را انتخاب نمایید.", "صنف انتخاب نشده", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (_classStudents.Count == 0)
        {
            MessageBox.Show("در این صنف هیچ شاگردی برای صدور کارنامه وجود ندارد.", "عدم وجود شاگرد", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            var year = GetAcademicYear();
            var reportType = GetSelectedReportType();
            var paths = await _reportCardService.GenerateClassReportCardsPdfAsync(classId, year, _appFolders.Reports, reportType);

            StatusTextBlock.Text = $"✅ تعداد {paths.Count} فایل کارنامه PDF برای این صنف صادر گردید.";
            MessageBox.Show($"تعداد {paths.Count} کارنامه PDF با موفقیت در پوشه Reports ایجاد گردید.", "صدور دسته‌جمعی موفق", MessageBoxButton.OK, MessageBoxImage.Information);

            OpenFolder(_appFolders.Reports);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطا در صدور دسته‌جمعی کارنامه‌ها:\n{ex.Message}", "خطا در تولید PDF", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OpenGeneratedPdfButton_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(_lastGeneratedPdfPath) && File.Exists(_lastGeneratedPdfPath))
        {
            OpenPdf(_lastGeneratedPdfPath);
        }
    }

    private void OpenReportsFolderButton_Click(object sender, RoutedEventArgs e)
    {
        Directory.CreateDirectory(_appFolders.Reports);
        OpenFolder(_appFolders.Reports);
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        await LoadClassesAsync();
        ApplyPermissionUi();
    }

    private static void OpenPdf(string filePath)
    {
        try
        {
            Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"امکان باز کردن مستقیم فایل وجود ندارد:\n{ex.Message}", "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private static void OpenFolder(string folderPath)
    {
        try
        {
            Process.Start(new ProcessStartInfo("explorer.exe", folderPath) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"امکان باز کردن پوشه وجود ندارد:\n{ex.Message}", "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
