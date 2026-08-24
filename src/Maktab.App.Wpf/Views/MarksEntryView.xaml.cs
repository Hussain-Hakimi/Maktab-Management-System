using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using Maktab.Application.Abstractions;
using Maktab.Domain.Entities;
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
    private readonly IAcademicYearService _academicYearService;
    private readonly ITeacherAssignmentService _teacherAssignmentService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditService _auditService;

    private readonly ObservableCollection<EditableMarkRowItem> _markRows = [];
    private readonly List<SchoolClass> _classes = [];
    private readonly List<Subject> _subjects = [];
    private bool _isGuardianOfSelectedClass;

    public MarksEntryView(
        IExamMarkService examMarkService,
        IClassSubjectService classSubjectService,
        IAcademicYearService academicYearService,
        ITeacherAssignmentService teacherAssignmentService,
        ICurrentUserService currentUserService,
        IAuditService auditService)
    {
        _examMarkService = examMarkService;
        _classSubjectService = classSubjectService;
        _academicYearService = academicYearService;
        _teacherAssignmentService = teacherAssignmentService;
        _currentUserService = currentUserService;
        _auditService = auditService;

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
            var allClasses = await _classSubjectService.GetClassesAsync();
            var currentUser = _currentUserService.CurrentUser;

            if (currentUser?.Role == Domain.Enums.UserRole.Admin)
            {
                _classes.Clear();
                _classes.AddRange(allClasses);
            }
            else if (currentUser is not null)
            {
                // Teacher or other role: show only classes where user has assignment or is guardian
                var assignments = await _teacherAssignmentService.GetMyTeacherSubjectsAsync(currentUser.UserId);
                var guardianships = await _teacherAssignmentService.GetClassGuardiansAsync(currentUser.UserId);

                var assignedClassIds = assignments.Select(a => a.ClassId).ToHashSet();
                var guardianClassIds = guardianships.Select(g => g.ClassId).ToHashSet();
                var allowedClassIds = assignedClassIds.Union(guardianClassIds).ToHashSet();

                _classes.Clear();
                _classes.AddRange(allClasses.Where(c => allowedClassIds.Contains(c.ClassId)));
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
                _subjects.Clear();
                SubjectComboBox.ItemsSource = null;
                _markRows.Clear();
                StudentCountTextBlock.Text = "شما به هیچ صنفی تخصیص نشده‌اید.";
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
            var allSubjects = await _classSubjectService.GetSubjectsByClassAsync(classId);
            var currentUser = _currentUserService.CurrentUser;

            if (currentUser?.Role == Domain.Enums.UserRole.Admin)
            {
                _subjects.Clear();
                _subjects.AddRange(allSubjects);
                _isGuardianOfSelectedClass = true; // admin can edit all
            }
            else if (currentUser is not null)
            {
                var assignments = await _teacherAssignmentService.GetMyTeacherSubjectsAsync(currentUser.UserId);
                var assignedSubjectIds = assignments
                    .Where(a => a.ClassId == classId)
                    .Select(a => a.SubjectId)
                    .ToHashSet();

                bool isGuardian = await _teacherAssignmentService.IsClassGuardianAsync(currentUser.UserId, classId);
                _isGuardianOfSelectedClass = isGuardian;

                if (isGuardian)
                {
                    // Guardian can see all subjects, but only edit own subjects
                    _subjects.Clear();
                    _subjects.AddRange(allSubjects);
                }
                else
                {
                    // Regular teacher sees only assigned subjects
                    _subjects.Clear();
                    _subjects.AddRange(allSubjects.Where(s => assignedSubjectIds.Contains(s.SubjectId)));
                }
            }
            else
            {
                _subjects.Clear();
                _isGuardianOfSelectedClass = false;
            }

            SubjectComboBox.ItemsSource = _subjects.ToList();
            if (_subjects.Count > 0)
            {
                SubjectComboBox.SelectedIndex = 0;
            }
            else
            {
                _markRows.Clear();
                StudentCountTextBlock.Text = "برای این صنف هیچ مضمونی تخصیص نشده است.";
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

            // If guardian and selected subject not assigned, show note
            var currentUser = _currentUserService.CurrentUser;
            if (currentUser?.Role == Domain.Enums.UserRole.Teacher && _isGuardianOfSelectedClass)
            {
                var assignments = await _teacherAssignmentService.GetMyTeacherSubjectsAsync(currentUser.UserId);
                bool canEdit = assignments.Any(a => a.ClassId == classId && a.SubjectId == subjectId);
                if (!canEdit)
                {
                    SaveStatusTextBlock.Text = "⚠️ شما فقط می‌توانید نمرات مضامین تدریسی خود را ویرایش کنید.";
                    SaveMarksButton.IsEnabled = false;
                }
                else
                {
                    SaveMarksButton.IsEnabled = true;
                }
            }
            else
            {
                SaveMarksButton.IsEnabled = true;
            }
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

        // Validate ranges
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
            var currentUser = _currentUserService.CurrentUser;
            if (currentUser?.Role == Domain.Enums.UserRole.Teacher)
            {
                int classId = (int)ClassComboBox.SelectedValue;
                int subjectId = (int)SubjectComboBox.SelectedValue;
                var assignments = await _teacherAssignmentService.GetMyTeacherSubjectsAsync(currentUser.UserId);
                bool canEdit = assignments.Any(a => a.ClassId == classId && a.SubjectId == subjectId);
                if (!canEdit)
                {
                    MessageBox.Show("شما اجازه ویرایش نمرات این مضمون را ندارید.", "دسترسی محدود", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            // Get active academic year
            var activeYear = await _academicYearService.GetActiveAcademicYearAsync();
            if (activeYear is null)
            {
                MessageBox.Show("سال تعلیمی فعال تعیین نشده است.", "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int classIdSave = (int)ClassComboBox.SelectedValue;
            int subjectIdSave = (int)SubjectComboBox.SelectedValue;

            var dtos = _markRows.Select(r => new SaveExamMarkDto(
                StudentId: r.StudentId,
                SubjectId: subjectIdSave,
                MidtermScore: r.MidtermScore,
                FinalScore: r.FinalScore,
                AcademicYearId: activeYear.AcademicYearId
            )).ToList();

            await _examMarkService.SaveMarksBatchAsync(dtos);
            await LogAuditAsync($"ثبت نمرات برای {_markRows.Count} شاگرد");
            SaveStatusTextBlock.Text = $"✅ تمام نمرات این مضمون با موفقیت در دیتابیس ذخیره شدند. ({DateTime.Now:HH:mm:ss})";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطا در ذخیره نمرات:\n{ex.Message}", "خطا در ذخیره", MessageBoxButton.OK, MessageBoxImage.Error);
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
            // Audit logging should not break saving marks
        }
    }
}
