using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using Maktab.Application.Abstractions;
using Maktab.Domain.Entities;
using Maktab.Domain.Enums;
using Maktab.Domain.Rules;

namespace Maktab.App.Wpf.Views;

public sealed class EditableMarkRowItem : INotifyPropertyChanged
{
    private decimal _midtermScore;
    private decimal _finalScore;
    private decimal _totalScore;
    private bool _isPass;

    public int StudentId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FatherName { get; set; } = string.Empty;
    public string RollNumber { get; set; } = string.Empty;
    public int SubjectId { get; set; }

    public decimal MidtermScore
    {
        get => _midtermScore;
        set
        {
            if (_midtermScore != value)
            {
                _midtermScore = value;
                OnPropertyChanged();
                Recalculate();
            }
        }
    }

    public decimal FinalScore
    {
        get => _finalScore;
        set
        {
            if (_finalScore != value)
            {
                _finalScore = value;
                OnPropertyChanged();
                Recalculate();
            }
        }
    }

    public decimal TotalScore
    {
        get => _totalScore;
        private set
        {
            if (_totalScore != value)
            {
                _totalScore = value;
                OnPropertyChanged();
            }
        }
    }

    public bool IsPass
    {
        get => _isPass;
        private set
        {
            if (_isPass != value)
            {
                _isPass = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ResultText));
            }
        }
    }

    public string ResultText => IsPass ? "کامیاب (Pass)" : "ناکام (Fail)";

    public void Recalculate()
    {
        var clampedMidterm = Math.Clamp(_midtermScore, 0m, GradingPolicy.MidtermMax);
        var clampedFinal = Math.Clamp(_finalScore, 0m, GradingPolicy.FinalMax);

        var total = clampedMidterm + clampedFinal;
        TotalScore = total;
        IsPass = total >= GradingPolicy.PassingMark;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public partial class MarksEntryView : UserControl
{
    private readonly IExamMarkService _examMarkService;
    private readonly IClassSubjectService _classSubjectService;

    private readonly ObservableCollection<EditableMarkRowItem> _markRows = [];
    private readonly List<SchoolClass> _classes = [];
    private readonly List<Subject> _subjects = [];

    public MarksEntryView(IExamMarkService examMarkService, IClassSubjectService classSubjectService)
    {
        _examMarkService = examMarkService;
        _classSubjectService = classSubjectService;

        InitializeComponent();

        MarksDataGrid.ItemsSource = _markRows;
        Loaded += MarksEntryView_Loaded;
    }

    private async void MarksEntryView_Loaded(object sender, RoutedEventArgs e)
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
        if (ClassComboBox.SelectedValue is int classId && classId > 0)
        {
            await LoadSubjectsForClassAsync(classId);
        }
        else
        {
            _subjects.Clear();
            SubjectComboBox.ItemsSource = null;
            _markRows.Clear();
            StudentCountTextBlock.Text = "شاگردان: ۰ نفر";
        }
    }

    private async Task LoadSubjectsForClassAsync(int classId)
    {
        try
        {
            var subjects = await _classSubjectService.GetSubjectsByClassAsync(classId);
            _subjects.Clear();
            _subjects.AddRange(subjects);

            SubjectComboBox.ItemsSource = _subjects.ToList();
            if (_subjects.Count > 0)
            {
                SubjectComboBox.SelectedIndex = 0;
            }
            else
            {
                _markRows.Clear();
                StudentCountTextBlock.Text = "برای این صنف هیچ مضمونی ثبت نشده است.";
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطا در دریافت مضامین:\n{ex.Message}", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void SubjectComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        await LoadMarksAsync();
    }

    private async Task LoadMarksAsync()
    {
        if (ClassComboBox.SelectedValue is not int classId || classId <= 0) return;
        if (SubjectComboBox.SelectedValue is not int subjectId || subjectId <= 0) return;

        try
        {
            var marks = await _examMarkService.GetClassSubjectMarksAsync(classId, subjectId);
            _markRows.Clear();

            foreach (var m in marks)
            {
                var row = new EditableMarkRowItem
                {
                    StudentId = m.StudentId,
                    FirstName = m.FirstName,
                    LastName = m.LastName,
                    FatherName = m.FatherName,
                    RollNumber = m.RollNumber,
                    SubjectId = subjectId,
                    MidtermScore = m.MidtermScore,
                    FinalScore = m.FinalScore
                };
                row.Recalculate();
                _markRows.Add(row);
            }

            StudentCountTextBlock.Text = $"تعداد شاگردان: {_markRows.Count} نفر";
            SaveStatusTextBlock.Text = string.Empty;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطا در بارگذاری جدول نمرات:\n{ex.Message}", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void MarksDataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
    {
        if (e.Row.Item is EditableMarkRowItem item)
        {
            Dispatcher.InvokeAsync(() => item.Recalculate());
        }
    }

    private async void ReloadButton_Click(object sender, RoutedEventArgs e)
    {
        await LoadMarksAsync();
    }

    private async void SaveMarksButton_Click(object sender, RoutedEventArgs e)
    {
        if (_markRows.Count == 0)
        {
            MessageBox.Show("هیچ شاگردی برای ثبت نمره وجود ندارد.", "توجه", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        // Validate all ranges before saving
        foreach (var row in _markRows)
        {
            if (row.MidtermScore < 0m || row.MidtermScore > GradingPolicy.MidtermMax)
            {
                MessageBox.Show($"نمره چهارونیم‌ماهه برای شاگرد «{row.FirstName} {row.LastName}» باید بین ۰ تا {GradingPolicy.MidtermMax} باشد.", "ورودی نامعتبر", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (row.FinalScore < 0m || row.FinalScore > GradingPolicy.FinalMax)
            {
                MessageBox.Show($"نمره سالانه برای شاگرد «{row.FirstName} {row.LastName}» باید بین ۰ تا {GradingPolicy.FinalMax} باشد.", "ورودی نامعتبر", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
        }

        try
        {
            var dtos = _markRows.Select(r => new SaveExamMarkDto(
                StudentId: r.StudentId,
                SubjectId: r.SubjectId,
                MidtermScore: r.MidtermScore,
                FinalScore: r.FinalScore
            )).ToList();

            await _examMarkService.SaveMarksBatchAsync(dtos);

            SaveStatusTextBlock.Text = $"✅ تمام نمرات این مضمون با موفقیت در دیتابیس ذخیره شدند. ({DateTime.Now:HH:mm:ss})";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطا در ذخیره نمرات:\n{ex.Message}", "خطا در ذخیره", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
