using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using Maktab.Application.Abstractions;
using Maktab.Domain.Entities;
using Maktab.Domain.Enums;

namespace Maktab.App.Wpf.Views;

public partial class AttendanceView : UserControl
{
    public static readonly IReadOnlyList<AttendanceStatus> Statuses =
        Enum.GetValues<AttendanceStatus>();

    private readonly IAttendanceService _attendanceService;
    private readonly IClassSubjectService _classSubjectService;

    private readonly ObservableCollection<EditableAttendanceRowItem> _attendanceRows = [];
    private readonly List<SchoolClass> _classes = [];

    public AttendanceView(
        IAttendanceService attendanceService,
        IClassSubjectService classSubjectService)
    {
        _attendanceService = attendanceService;
        _classSubjectService = classSubjectService;

        InitializeComponent();

        AttendanceDataGrid.ItemsSource = _attendanceRows;
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
            if (_classes.Count > 0)
            {
                ClassComboBox.SelectedIndex = 0;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطا در دریافت صنف‌ها:\n{ex.Message}", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void ClassComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        await LoadAttendanceAsync();
    }

    private async void AttendanceDatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
    {
        await LoadAttendanceAsync();
    }

    private async Task LoadAttendanceAsync()
    {
        if (ClassComboBox.SelectedValue is not int classId || classId <= 0) return;
        if (AttendanceDatePicker.SelectedDate is not DateTime date) return;

        UpdateShamsiDateLabel(date);

        try
        {
            var records = await _attendanceService.GetClassAttendanceForDateAsync(classId, date);
            _attendanceRows.Clear();

            foreach (var record in records)
            {
                _attendanceRows.Add(new EditableAttendanceRowItem
                {
                    StudentId = record.StudentId,
                    FirstName = record.FirstName,
                    LastName = record.LastName,
                    FatherName = record.FatherName,
                    RollNumber = record.RollNumber,
                    Status = record.Status
                });
            }

            SaveStatusTextBlock.Text = string.Empty;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطا در بارگذاری حاضری:\n{ex.Message}", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void UpdateShamsiDateLabel(DateTime date)
    {
        try
        {
            var pc = new System.Globalization.PersianCalendar();
            var year = pc.GetYear(date);
            var month = pc.GetMonth(date);
            var day = pc.GetDayOfMonth(date);
            ShamsiDateTextBlock.Text = $"{year}/{month:D2}/{day:D2}";
        }
        catch
        {
            ShamsiDateTextBlock.Text = "";
        }
    }

    private async void ReloadButton_Click(object sender, RoutedEventArgs e)
    {
        await LoadAttendanceAsync();
    }

    private async void SaveAttendanceButton_Click(object sender, RoutedEventArgs e)
    {
        if (_attendanceRows.Count == 0)
        {
            MessageBox.Show("هیچ شاگردی برای ثبت حاضری وجود ندارد.", "توجه", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (AttendanceDatePicker.SelectedDate is not DateTime date)
        {
            MessageBox.Show("لطفاً تاریخ را انتخاب کنید.", "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var dtos = _attendanceRows.Select(r => new SaveAttendanceDto(
                StudentId: r.StudentId,
                Date: date,
                Status: r.Status
            )).ToList();

            await _attendanceService.SaveAttendanceBatchAsync(dtos);

            SaveStatusTextBlock.Text = $"✅ حاضری تمام شاگردان برای {date:yyyy/MM/dd} با موفقیت ذخیره شد.";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطا در ذخیره حاضری:\n{ex.Message}", "خطا در ذخیره", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}

public sealed class EditableAttendanceRowItem : INotifyPropertyChanged
{
    private AttendanceStatus _status;

    public int StudentId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FatherName { get; set; } = string.Empty;
    public string RollNumber { get; set; } = string.Empty;

    public AttendanceStatus Status
    {
        get => _status;
        set
        {
            if (_status != value)
            {
                _status = value;
                OnPropertyChanged();
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
