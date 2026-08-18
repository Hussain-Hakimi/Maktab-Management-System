using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Maktab.Application.Abstractions;
using Maktab.Domain.Entities;
using Maktab.Domain.Enums;

namespace Maktab.App.Wpf.Views;

public sealed class AttendanceStatusChoice
{
    public AttendanceStatus Value { get; set; }
    public string Label { get; set; } = string.Empty;
}

public sealed class AttendanceSummaryDisplayItem
{
    public int StudentId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string RollNumber { get; set; } = string.Empty;
    public int PresentDays { get; set; }
    public int AbsentDays { get; set; }
    public int IllDays { get; set; }
    public int PermissionDays { get; set; }
    public int TotalRecordedDays { get; set; }
    public bool ExceedsAbsenceLimit { get; set; }
    public string AbsenceLimitStatus => ExceedsAbsenceLimit ? "⚠️ غیبت بیش از ۳۰ روز" : "✔️ در حد مجاز";
}

public partial class AttendanceView : UserControl
{
    private const int TemplateDays = 30;

    private readonly IAttendanceService _attendanceService;
    private readonly IAttendanceExcelService _attendanceExcelService;
    private readonly IClassSubjectService _classSubjectService;

    private readonly ObservableCollection<DailyAttendanceRowDto> _sheetRows = [];
    private readonly ObservableCollection<AttendanceSummaryDisplayItem> _summaryRows = [];
    private readonly List<SchoolClass> _classes = [];

    public AttendanceView(
        IAttendanceService attendanceService,
        IAttendanceExcelService attendanceExcelService,
        IClassSubjectService classSubjectService)
    {
        _attendanceService = attendanceService;
        _attendanceExcelService = attendanceExcelService;
        _classSubjectService = classSubjectService;

        InitializeComponent();

        AttendanceDataGrid.ItemsSource = _sheetRows;
        StatsDataGrid.ItemsSource = _summaryRows;
        StatusColumn.ItemsSource = new List<AttendanceStatusChoice>
        {
            new() { Value = AttendanceStatus.Present, Label = "حاضر" },
            new() { Value = AttendanceStatus.Absent, Label = "غیرحاضر" },
            new() { Value = AttendanceStatus.Ill, Label = "مریض" },
            new() { Value = AttendanceStatus.Permission, Label = "اجازه" }
        };

        AttendanceDatePicker.SelectedDate = DateTime.Today;
        Loaded += AttendanceView_Loaded;
    }

    private async void AttendanceView_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadClassesAsync();
    }

    public async Task InitializeDataAsync()
    {
        await LoadClassesAsync();
    }

    private async Task LoadClassesAsync()
    {
        try
        {
            var classes = await _classSubjectService.GetClassesAsync();
            _classes.Clear();
            _classes.AddRange(classes);

            ClassComboBox.ItemsSource = _classes.ToList();
            StatsClassComboBox.ItemsSource = _classes.ToList();

            if (_classes.Count > 0)
            {
                ClassComboBox.SelectedIndex = 0;
                StatsClassComboBox.SelectedIndex = 0;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطا در دریافت لیست صنف‌ها:\n{ex.Message}", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private int? GetSelectedClassId() => ClassComboBox.SelectedValue is int id && id > 0 ? id : null;

    private DateOnly GetSelectedDate() =>
        DateOnly.FromDateTime(AttendanceDatePicker.SelectedDate ?? DateTime.Today);

    private async void LoadSheetButton_Click(object sender, RoutedEventArgs e)
    {
        await LoadSheetAsync();
    }

    private async Task LoadSheetAsync()
    {
        var classId = GetSelectedClassId();
        if (classId is null)
        {
            MessageBox.Show("لطفاً ابتدا یک صنف را انتخاب کنید.", "صنف انتخاب نشده", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            var rows = await _attendanceService.GetDailySheetAsync(classId.Value, GetSelectedDate());
            _sheetRows.Clear();
            foreach (var row in rows)
            {
                _sheetRows.Add(row);
            }

            SheetCountTextBlock.Text = $"تعداد شاگردان: {_sheetRows.Count}";
            SheetStatusTextBlock.Text = _sheetRows.Any(r => r.IsSaved)
                ? "ℹ️ برای این تاریخ قبلاً حاضری ثبت شده است — تغییرات شما آن را به‌روز می‌کند."
                : string.Empty;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطا در بارگذاری حاضری:\n{ex.Message}", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void SaveSheetButton_Click(object sender, RoutedEventArgs e)
    {
        var classId = GetSelectedClassId();
        if (classId is null)
        {
            MessageBox.Show("لطفاً ابتدا یک صنف را انتخاب و حاضری را بارگذاری کنید.", "صنف انتخاب نشده", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (_sheetRows.Count == 0)
        {
            MessageBox.Show("لیست حاضری خالی است. ابتدا «بارگذاری حاضری» را بزنید.", "لیست خالی", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var date = GetSelectedDate();
        try
        {
            var records = _sheetRows.Select(r => new SaveAttendanceDto(r.StudentId, date, r.Status, r.Notes));
            await _attendanceService.SaveDailySheetAsync(records);
            SheetStatusTextBlock.Text = $"✅ حاضری تاریخ {date:yyyy-MM-dd} برای {_sheetRows.Count} شاگرد با موفقیت ثبت شد.";
            await LoadSheetAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"ثبت حاضری ناموفق:\n{ex.Message}", "خطا در ثبت", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void MarkAllPresentButton_Click(object sender, RoutedEventArgs e)
    {
        foreach (var row in _sheetRows)
        {
            row.Status = AttendanceStatus.Present;
        }

        AttendanceDataGrid.Items.Refresh();
        SheetStatusTextBlock.Text = "✅ همه شاگردان «حاضر» علامت شدند — فراموش نکنید که «ثبت حاضری» را بزنید.";
    }

    private async void DownloadTemplateButton_Click(object sender, RoutedEventArgs e)
    {
        var classId = GetSelectedClassId();
        if (classId is null)
        {
            MessageBox.Show("لطفاً ابتدا یک صنف را انتخاب کنید.", "صنف انتخاب نشده", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dialog = new SaveFileDialog
        {
            Title = "ذخیره قالب حاضری اکسل",
            Filter = "Excel Workbook (*.xlsx)|*.xlsx",
            FileName = $"attendance-class{classId}-{GetSelectedDate():yyyyMM}.xlsx"
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            var startDate = new DateOnly(GetSelectedDate().Year, GetSelectedDate().Month, 1);
            await _attendanceExcelService.GenerateClassTemplateAsync(classId.Value, startDate, TemplateDays, dialog.FileName);
            MessageBox.Show(
                $"قالب اکسل {TemplateDays} روزه ساخته شد:\n{dialog.FileName}\n\nتمام خانواده‌ها به‌صورت پیش‌فرض «حاضر» است — فقط خانواده‌های غیرحاضر/مریض/اجازه را تغییر دهید و سپس از دکمه «وارد کردن از اکسل» استفاده نمایید.",
                "قالب آماده شد",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"ساخت قالب ناموفق:\n{ex.Message}", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void ImportExcelButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "انتخاب فایل اکسل حاضری",
            Filter = "Excel Workbook (*.xlsx)|*.xlsx"
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            var result = await _attendanceExcelService.ImportTemplateAsync(dialog.FileName);

            if (result.Rows.Count == 0)
            {
                MessageBox.Show("هیچ رکورد قابل ثبتی در فایل پیدا نشد.", "فایل خالی", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (result.HasErrors)
            {
                var preview = string.Join("\n", result.Errors.Take(10));
                var more = result.Errors.Count > 10 ? $"\n... و {result.Errors.Count - 10} مورد دیگر" : string.Empty;
                MessageBox.Show(
                    $"هشدار: {result.Errors.Count} مشکل در فایل پیدا شد. رکوردهای معتبر ثبت می‌شوند:\n\n{preview}{more}",
                    "هشدار وارد کردن",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }

            var confirm = MessageBox.Show(
                $"{result.Rows.Count} رکورد حاضری برای صنف «{result.ClassName}» پیدا شد. آیا ثبت شود؟",
                "تأیید وارد کردن حاضری",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes)
            {
                return;
            }

            await _attendanceService.SaveDailySheetAsync(result.Rows);
            SheetStatusTextBlock.Text = $"✅ {result.Rows.Count} رکورد حاضری از اکسل وارد و ثبت شد.";

            if (ClassComboBox.SelectedValue is int currentClassId && currentClassId == result.ClassId)
            {
                await LoadSheetAsync();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"وارد کردن از اکسل ناموفق:\n{ex.Message}", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void LoadStatsButton_Click(object sender, RoutedEventArgs e)
    {
        if (StatsClassComboBox.SelectedValue is not int classId || classId <= 0)
        {
            MessageBox.Show("لطفاً یک صنف را انتخاب کنید.", "صنف انتخاب نشده", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            var summaries = await _attendanceService.GetClassSummaryAsync(classId);
            _summaryRows.Clear();
            foreach (var s in summaries)
            {
                _summaryRows.Add(new AttendanceSummaryDisplayItem
                {
                    StudentId = s.StudentId,
                    FirstName = s.FirstName,
                    LastName = s.LastName,
                    RollNumber = s.RollNumber,
                    PresentDays = s.PresentDays,
                    AbsentDays = s.AbsentDays,
                    IllDays = s.IllDays,
                    PermissionDays = s.PermissionDays,
                    TotalRecordedDays = s.TotalRecordedDays,
                    ExceedsAbsenceLimit = s.ExceedsAbsenceLimit
                });
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطا در بارگذاری احصائیه:\n{ex.Message}", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
