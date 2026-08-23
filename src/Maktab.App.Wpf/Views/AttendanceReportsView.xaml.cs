using System.Collections.ObjectModel;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using Maktab.Application.Abstractions;
using Maktab.Domain.Entities;

namespace Maktab.App.Wpf.Views;

public partial class AttendanceReportsView : UserControl
{
    private readonly IAttendanceService _attendanceService;
    private readonly IClassSubjectService _classSubjectService;
    private readonly IAcademicYearService _academicYearService;

    private readonly ObservableCollection<StudentAttendanceSummaryDto> _summaryRows = [];
    private DataTable? _monthlyTable;

    public AttendanceReportsView(
        IAttendanceService attendanceService,
        IClassSubjectService classSubjectService,
        IAcademicYearService academicYearService)
    {
        _attendanceService = attendanceService;
        _classSubjectService = classSubjectService;
        _academicYearService = academicYearService;

        InitializeComponent();
        Loaded += AttendanceReportsView_Loaded;
    }

    private async void AttendanceReportsView_Loaded(object sender, RoutedEventArgs e)
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

            var months = Enumerable.Range(1, 12).Select(m => new ComboBoxItem { Content = $"{m}", Tag = m }).ToList();
            MonthComboBox.ItemsSource = months;
            MonthComboBox.SelectedIndex = DateTime.Today.Month - 1;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطا در بارگذاری فیلترها:\n{ex.Message}", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void ClassComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // No need to reload; only used for filtering
    }

    private async void ShowSummaryButton_Click(object sender, RoutedEventArgs e)
    {
        if (ClassComboBox.SelectedValue is not int classId || classId <= 0)
        {
            MessageBox.Show("لطفاً صنف را انتخاب کنید.", "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (AcademicYearComboBox.SelectedValue is not int yearId || yearId <= 0)
        {
            MessageBox.Show("لطفاً سال تعلیمی را انتخاب کنید.", "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var summary = await _attendanceService.GetClassAttendanceSummaryAsync(classId, yearId);
            _summaryRows.Clear();
            foreach (var row in summary)
                _summaryRows.Add(row);

            ReportDataGrid.ItemsSource = _summaryRows;
            ReportDataGrid.AutoGenerateColumns = true;
            TitleTextBlock.Text = "خلاصه حاضری";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطا در بارگذاری خلاصه:\n{ex.Message}", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void ShowMonthlyButton_Click(object sender, RoutedEventArgs e)
    {
        if (ClassComboBox.SelectedValue is not int classId || classId <= 0)
        {
            MessageBox.Show("لطفاً صنف را انتخاب کنید.", "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (AcademicYearComboBox.SelectedValue is not int yearId || yearId <= 0)
        {
            MessageBox.Show("لطفاً سال تعلیمی را انتخاب کنید.", "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (MonthComboBox.SelectedItem is not ComboBoxItem monthItem || monthItem.Tag is not int month)
        {
            MessageBox.Show("لطفاً ماه را انتخاب کنید.", "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Get Gregorian year from academic year start? We'll use current year for simplicity.
        int year = DateTime.Today.Year;
        try
        {
            var rows = await _attendanceService.GetMonthlyAttendanceReportAsync(classId, year, month, yearId);
            _monthlyTable = BuildMonthlyDataTable(rows);
            ReportDataGrid.ItemsSource = _monthlyTable.DefaultView;
            ReportDataGrid.AutoGenerateColumns = true;
            TitleTextBlock.Text = $"گزارش ماهانه {month}";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطا در بارگذاری گزارش ماهانه:\n{ex.Message}", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static DataTable BuildMonthlyDataTable(IReadOnlyList<MonthlyAttendanceRowDto> rows)
    {
        var table = new DataTable();
        table.Columns.Add("نام شاگرد", typeof(string));
        table.Columns.Add("شماره اساس", typeof(string));

        int days = rows.FirstOrDefault()?.DayStatuses.Keys.Max() ?? 30;
        for (int day = 1; day <= days; day++)
        {
            table.Columns.Add($"روز {day}", typeof(string));
        }

        foreach (var row in rows)
        {
            var dr = table.NewRow();
            dr["نام شاگرد"] = row.StudentName;
            dr["شماره اساس"] = row.RollNumber;
            for (int day = 1; day <= days; day++)
            {
                var status = row.DayStatuses.TryGetValue(day, out var s) ? s.ToString() : "Present";
                dr[$"روز {day}"] = status;
            }
            table.Rows.Add(dr);
        }

        return table;
    }
}
