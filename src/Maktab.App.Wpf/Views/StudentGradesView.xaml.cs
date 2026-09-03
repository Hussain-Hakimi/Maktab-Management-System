using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using Maktab.Application.Abstractions;
using Maktab.Domain.Entities;
using Maktab.Domain.Rules;

namespace Maktab.App.Wpf.Views;

public sealed class StudentGradeRowItem : INotifyPropertyChanged
{
    private decimal _midtermScore;
    private decimal _finalScore;
    private decimal _totalScore;
    private bool _isPass;

    public int StudentId { get; set; }
    public int SubjectId { get; set; }
    public string SubjectName { get; set; } = string.Empty;

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

    public string ResultText => IsPass ? "کامیاب" : "ناکام";

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
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public partial class StudentGradesView : UserControl
{
    private readonly IStudentService _studentService;
    private readonly IClassSubjectService _classSubjectService;
    private readonly IAcademicYearService _academicYearService;
    private readonly IExamMarkService _examMarkService;
    private readonly ITeacherAssignmentService _teacherAssignmentService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditService _auditService;
    private readonly IFinalizationService _finalizationService;

    private readonly ObservableCollection<StudentGradeRowItem> _rows = [];
    private List<Student> _students = [];

    public StudentGradesView(
        IStudentService studentService,
        IClassSubjectService classSubjectService,
        IAcademicYearService academicYearService,
        IExamMarkService examMarkService,
        ITeacherAssignmentService teacherAssignmentService,
        ICurrentUserService currentUserService,
        IAuditService auditService,
        IFinalizationService finalizationService)
    {
        _studentService = studentService;
        _classSubjectService = classSubjectService;
        _academicYearService = academicYearService;
        _examMarkService = examMarkService;
        _teacherAssignmentService = teacherAssignmentService;
        _currentUserService = currentUserService;
        _auditService = auditService;
        _finalizationService = finalizationService;

        InitializeComponent();
        GradesDataGrid.ItemsSource = _rows;
        Loaded += StudentGradesView_Loaded;
    }

    private async void StudentGradesView_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadClassesAsync();
        await LoadAcademicYearsAsync();
    }

    private async Task LoadClassesAsync()
    {
        try
        {
            var allClasses = await _classSubjectService.GetClassesAsync();
            var currentUser = _currentUserService.CurrentUser;

            if (currentUser?.Role == Domain.Enums.UserRole.Admin)
            {
                ClassComboBox.ItemsSource = allClasses;
            }
            else if (currentUser is not null)
            {
                var assignments = await _teacherAssignmentService.GetMyTeacherSubjectsAsync(currentUser.UserId);
                var guardianships = await _teacherAssignmentService.GetClassGuardiansAsync(currentUser.UserId);
                var allowedClassIds = assignments.Select(a => a.ClassId)
                    .Union(guardianships.Select(g => g.ClassId))
                    .ToHashSet();
                ClassComboBox.ItemsSource = allClasses.Where(c => allowedClassIds.Contains(c.ClassId)).ToList();
            }
            else
            {
                ClassComboBox.ItemsSource = null;
            }

            if (ClassComboBox.Items.Count > 0)
                ClassComboBox.SelectedIndex = 0;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطا در بارگذاری صنف‌ها:\n{ex.Message}", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task LoadAcademicYearsAsync()
    {
        try
        {
            var years = await _academicYearService.GetAllAcademicYearsAsync();
            AcademicYearComboBox.ItemsSource = years;
            var active = years.FirstOrDefault(y => y.IsActive);
            AcademicYearComboBox.SelectedItem = active ?? years.FirstOrDefault();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطا در بارگذاری سال‌ها:\n{ex.Message}", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void ClassComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ClassComboBox.SelectedValue is int classId && classId > 0)
        {
            await LoadStudentsAsync(classId);
        }
        else
        {
            _students.Clear();
            StudentComboBox.ItemsSource = null;
        }
    }

    private async Task LoadStudentsAsync(int classId)
    {
        try
        {
            _students = (await _studentService.GetStudentsByClassAsync(classId)).ToList();
            StudentComboBox.ItemsSource = _students.Select(s => new StudentComboItem
            {
                StudentId = s.StudentId,
                DisplayName = $"{s.FirstName} {s.LastName} (اساس: {s.RollNumber})"
            }).ToList();
            if (_students.Count > 0) StudentComboBox.SelectedIndex = 0;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطا در بارگذاری شاگردان:\n{ex.Message}", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void StudentComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        await LoadMarksAsync();
    }

    private async void AcademicYearComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        await LoadMarksAsync();
    }

    private async Task LoadMarksAsync()
    {
        if (StudentComboBox.SelectedValue is not int studentId || studentId <= 0) return;
        if (AcademicYearComboBox.SelectedValue is not int yearId || yearId <= 0) return;

        try
        {
            var marks = await _examMarkService.GetStudentMarksForYearAsync(studentId, yearId);

            var currentUser = _currentUserService.CurrentUser;
            if (currentUser?.Role == Domain.Enums.UserRole.Teacher)
            {
                int classId = (int)ClassComboBox.SelectedValue;
                var assignments = await _teacherAssignmentService.GetMyTeacherSubjectsAsync(currentUser.UserId);
                var assignedSubjectIds = assignments
                    .Where(a => a.ClassId == classId)
                    .Select(a => a.SubjectId)
                    .ToHashSet();

                bool isGuardian = await _teacherAssignmentService.IsClassGuardianAsync(currentUser.UserId, classId);
                if (!isGuardian)
                {
                    marks = marks.Where(m => assignedSubjectIds.Contains(m.SubjectId)).ToList();
                }
            }

            _rows.Clear();
            foreach (var m in marks)
            {
                var row = new StudentGradeRowItem
                {
                    StudentId = m.StudentId,
                    SubjectId = m.SubjectId,
                    SubjectName = m.SubjectName,
                    MidtermScore = m.MidtermScore,
                    FinalScore = m.FinalScore
                };
                row.Recalculate();
                _rows.Add(row);
            }

            // Check finalization
            if (ClassComboBox.SelectedValue is int classIdFinal && AcademicYearComboBox.SelectedValue is int yearIdFinal)
            {
                bool isFinalized = await _finalizationService.IsClassFinalizedAsync(classIdFinal, yearIdFinal);
                if (isFinalized)
                {
                    SaveButton.IsEnabled = false;
                    SaveStatusTextBlock.Text = "🔒 نتایج این صنف نهایی شده است و قابل ویرایش نیست.";
                }
                else
                {
                    SaveButton.IsEnabled = true;
                    SaveStatusTextBlock.Text = string.Empty;
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطا در بارگذاری نمرات:\n{ex.Message}", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void GradesDataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
    {
        if (e.Row.Item is StudentGradeRowItem item)
            Dispatcher.InvokeAsync(() => item.Recalculate());
    }

    private async void ReloadButton_Click(object sender, RoutedEventArgs e)
    {
        await LoadMarksAsync();
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (_rows.Count == 0)
        {
            MessageBox.Show("هیچ مضمونی برای ذخیره وجود ندارد.", "توجه", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (StudentComboBox.SelectedValue is not int studentId || studentId <= 0)
        {
            MessageBox.Show("لطفاً شاگرد را انتخاب کنید.", "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (AcademicYearComboBox.SelectedValue is not int yearId || yearId <= 0)
        {
            MessageBox.Show("لطفاً سال تعلیمی را انتخاب کنید.", "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Validate ranges
        foreach (var row in _rows)
        {
            if (row.MidtermScore < 0m || row.MidtermScore > GradingPolicy.MidtermMax)
            {
                MessageBox.Show($"نمره چهارونیم‌ماهه برای مضمون «{row.SubjectName}» باید بین ۰ و {GradingPolicy.MidtermMax} باشد.", "ورودی نامعتبر", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (row.FinalScore < 0m || row.FinalScore > GradingPolicy.FinalMax)
            {
                MessageBox.Show($"نمره سالانه برای مضمون «{row.SubjectName}» باید بین ۰ و {GradingPolicy.FinalMax} باشد.", "ورودی نامعتبر", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
        }

        try
        {
            // Permission check
            var currentUser = _currentUserService.CurrentUser;
            int classId = (int)ClassComboBox.SelectedValue;
            if (currentUser?.Role == Domain.Enums.UserRole.Teacher)
            {
                var assignments = await _teacherAssignmentService.GetMyTeacherSubjectsAsync(currentUser.UserId);
                var assignedSubjectIds = assignments.Where(a => a.ClassId == classId).Select(a => a.SubjectId).ToHashSet();
                bool isGuardian = await _teacherAssignmentService.IsClassGuardianAsync(currentUser.UserId, classId);

                foreach (var row in _rows)
                {
                    if (!assignedSubjectIds.Contains(row.SubjectId) && !isGuardian)
                    {
                        MessageBox.Show("شما اجازه ویرایش نمرات این مضمون را ندارید.", "دسترسی محدود", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }
            }

            // Finalization check
            bool isFinalized = await _finalizationService.IsClassFinalizedAsync(classId, yearId);
            if (isFinalized)
            {
                MessageBox.Show("نتایج این صنف نهایی شده است و قابل ویرایش نیست.", "دسترسی محدود", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dtos = _rows.Select(r => new SaveExamMarkDto(
                StudentId: studentId,
                SubjectId: r.SubjectId,
                MidtermScore: r.MidtermScore,
                FinalScore: r.FinalScore,
                AcademicYearId: yearId
            )).ToList();

            await _examMarkService.SaveMarksBatchAsync(dtos);
            await LogAuditAsync($"ذخیره نمرات شاگرد آیدی {studentId} برای سال {yearId}");
            SaveStatusTextBlock.Text = $"✅ نمرات با موفقیت ذخیره شد. ({DateTime.Now:HH:mm:ss})";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطا در ذخیره نمرات:\n{ex.Message}", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
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
            // ignore audit failures
        }
    }
}
