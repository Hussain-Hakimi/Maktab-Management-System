using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Maktab.Application.Abstractions;

namespace Maktab.App.Wpf.Views;

public partial class ReportsView : UserControl
{
    private readonly IReportService _reportService;
    private readonly IExportService _exportService;
    private readonly IClassSubjectService _classSubjectService;
    private readonly IAcademicYearService _academicYearService;

    private readonly ObservableCollection<object> _previewItems = [];

    public ReportsView(
        IReportService reportService,
        IExportService exportService,
        IClassSubjectService classSubjectService,
        IAcademicYearService academicYearService)
    {
        _reportService = reportService;
        _exportService = exportService;
        _classSubjectService = classSubjectService;
        _academicYearService = academicYearService;

        InitializeComponent();
        PreviewDataGrid.ItemsSource = _previewItems;
        Loaded += ReportsView_Loaded;
    }

    private async void ReportsView_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadFiltersAsync();
    }

    private async Task LoadFiltersAsync()
    {
        try
        {
            var classes = await _classSubjectService.GetClassesAsync();
            ClassComboBox.ItemsSource = classes;
            if (classes.Count > 0) ClassComboBox.SelectedIndex = 0;

            var years = await _academicYearService.GetAllAcademicYearsAsync();
            AcademicYearComboBox.ItemsSource = years;
            var active = years.FirstOrDefault(y => y.IsActive);
            AcademicYearComboBox.SelectedItem = active ?? years.FirstOrDefault();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطا در بارگذاری فیلترها:\n{ex.Message}", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task LoadSubjectsAsync()
    {
        if (ClassComboBox.SelectedValue is not int classId || classId <= 0) return;
        try
        {
            var subjects = await _classSubjectService.GetSubjectsByClassAsync(classId);
            SubjectComboBox.ItemsSource = subjects;
            if (subjects.Count > 0) SubjectComboBox.SelectedIndex = 0;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطا در بارگذاری مضامین:\n{ex.Message}", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void ClassComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        await LoadSubjectsAsync();
    }

    private async Task<(int classId, int yearId)> GetSelectedFiltersAsync()
    {
        if (ClassComboBox.SelectedValue is not int classId || classId <= 0)
            throw new InvalidOperationException("صنف انتخاب نشده است.");
        if (AcademicYearComboBox.SelectedValue is not int yearId || yearId <= 0)
            throw new InvalidOperationException("سال تعلیمی انتخاب نشده است.");
        return (classId, yearId);
    }

    private async Task<string?> GetSaveFilePathAsync(string defaultName)
    {
        var dialog = new SaveFileDialog
        {
            Filter = "Excel Files (*.xlsx)|*.xlsx",
            FileName = defaultName,
            Title = "ذخیره فایل اکسل"
        };
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    private async Task ExportAsync<T>(IEnumerable<T> data, string sheetName, string fileName)
    {
        var filePath = await GetSaveFilePathAsync(fileName);
        if (filePath is null) return;

        try
        {
            await _exportService.ExportAsync(data, filePath, sheetName);
            _previewItems.Clear();
            foreach (var item in data) _previewItems.Add(item);
            MessageBox.Show("فایل اکسل با موفقیت ذخیره شد.", "موفق", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطا در خروجی اکسل:\n{ex.Message}", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void ExportClassPerformanceButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var (classId, yearId) = await GetSelectedFiltersAsync();
            var data = await _reportService.GetClassPerformanceAsync(classId, yearId);
            var rows = new List<SubjectPerformanceDto> { new() { SubjectName = "Overall", AverageScore = data.OverallAverage, PassCount = data.PassCount, FailCount = data.FailCount } };
            rows.AddRange(data.SubjectPerformances);
            await ExportAsync(rows, "ClassPerformance", $"{data.ClassName}_Performance.xlsx");
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "خطا", MessageBoxButton.OK, MessageBoxImage.Warning); }
    }

    private async void ExportGradeDistributionButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var (classId, yearId) = await GetSelectedFiltersAsync();
            var data = await _reportService.GetGradeDistributionAsync(classId, yearId);
            await ExportAsync([data], "GradeDistribution", $"{data.ClassName}_Distribution.xlsx");
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "خطا", MessageBoxButton.OK, MessageBoxImage.Warning); }
    }

    private async void ExportStudentsButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var (classId, _) = await GetSelectedFiltersAsync();
            var data = await _reportService.GetStudentExportDataAsync(classId);
            await ExportAsync(data, "Students", "Students.xlsx");
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "خطا", MessageBoxButton.OK, MessageBoxImage.Warning); }
    }

    private async void ExportMarksButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var (classId, yearId) = await GetSelectedFiltersAsync();
            if (SubjectComboBox.SelectedValue is not int subjectId || subjectId <= 0)
                throw new InvalidOperationException("مضمون انتخاب نشده است.");
            var data = await _reportService.GetMarkExportDataAsync(classId, subjectId, yearId);
            await ExportAsync(data, "Marks", "Marks.xlsx");
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "خطا", MessageBoxButton.OK, MessageBoxImage.Warning); }
    }

    private async void ExportAttendanceButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var (classId, yearId) = await GetSelectedFiltersAsync();
            var data = await _reportService.GetAttendanceExportDataAsync(classId, yearId);
            await ExportAsync(data, "Attendance", "Attendance.xlsx");
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "خطا", MessageBoxButton.OK, MessageBoxImage.Warning); }
    }

    private async void ExportFeesButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var (classId, yearId) = await GetSelectedFiltersAsync();
            var data = await _reportService.GetFeeExportDataAsync(classId, yearId);
            await ExportAsync(data, "Fees", "Fees.xlsx");
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "خطا", MessageBoxButton.OK, MessageBoxImage.Warning); }
    }
}
