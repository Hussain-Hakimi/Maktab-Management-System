using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Maktab.Application.Abstractions;
using Maktab.Domain.Entities;
using Maktab.Domain.Enums;
using Maktab.Domain.Rules;

namespace Maktab.App.Wpf.Views;

public sealed class GuardianStudentSummaryItem
{
    public int StudentId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string RollNumber { get; set; } = string.Empty;
    public decimal TotalScore { get; set; }
    public decimal AveragePercentage { get; set; }
    public string ResultText { get; set; } = string.Empty;
}

public partial class GuardianClassView : UserControl
{
    private readonly ITeacherAssignmentService _assignmentService;
    private readonly IClassSubjectService _classSubjectService;
    private readonly IStudentService _studentService;
    private readonly IExamMarkService _examMarkService;
    private readonly IAcademicYearService _academicYearService;
    private readonly ICurrentUserService _currentUserService;

    private readonly ObservableCollection<GuardianStudentSummaryItem> _students = [];
    private List<SchoolClass> _guardianClasses = [];

    public GuardianClassView(
        ITeacherAssignmentService assignmentService,
        IClassSubjectService classSubjectService,
        IStudentService studentService,
        IExamMarkService examMarkService,
        IAcademicYearService academicYearService,
        ICurrentUserService currentUserService)
    {
        _assignmentService = assignmentService;
        _classSubjectService = classSubjectService;
        _studentService = studentService;
        _examMarkService = examMarkService;
        _academicYearService = academicYearService;
        _currentUserService = currentUserService;

        InitializeComponent();
        StudentsDataGrid.ItemsSource = _students;
        Loaded += GuardianClassView_Loaded;
    }

    private async void GuardianClassView_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadGuardianClassesAsync();
        await LoadAcademicYearsAsync();
    }

    private async Task LoadGuardianClassesAsync()
    {
        try
        {
            var userId = _currentUserService.CurrentUser?.UserId;
            if (userId is null or <= 0)
                return;

            var guardianships = await _assignmentService.GetClassGuardiansAsync(userId.Value);
            var classIds = guardianships.Select(g => g.ClassId).ToList();

            var allClasses = await _classSubjectService.GetClassesAsync();
            _guardianClasses = allClasses.Where(c => classIds.Contains(c.ClassId)).ToList();

            ClassComboBox.ItemsSource = _guardianClasses;
            if (_guardianClasses.Count > 0)
                ClassComboBox.SelectedIndex = 0;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطا در بارگذاری صنف‌های نگرانی:\n{ex.Message}", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
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
        await LoadStudentsAsync();
    }

    private async void AcademicYearComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        await LoadStudentsAsync();
    }

    private async Task LoadStudentsAsync()
    {
        if (ClassComboBox.SelectedValue is not int classId || classId <= 0) return;
        if (AcademicYearComboBox.SelectedValue is not int yearId || yearId <= 0) return;

        try
        {
            var students = await _studentService.GetStudentsByClassAsync(classId);
            var subjects = await _classSubjectService.GetSubjectsByClassAsync(classId);

            _students.Clear();
            foreach (var student in students)
            {
                var marks = await _examMarkService.GetStudentMarksForYearAsync(student.StudentId, yearId);
                var markMap = marks.ToDictionary(m => m.SubjectId);

                decimal totalObtained = 0m;
                int failed = 0;
                foreach (var subject in subjects)
                {
                    markMap.TryGetValue(subject.SubjectId, out var mark);
                    var total = GradingPolicy.CalculateTotal(mark?.MidtermScore ?? 0m, mark?.FinalScore ?? 0m);
                    totalObtained += total;
                    if (!GradingPolicy.IsPass(total)) failed++;
                }

                var maxScore = subjects.Count * GradingPolicy.TotalMax;
                var average = maxScore > 0 ? Math.Round((totalObtained / maxScore) * 100m, 2) : 0m;
                var outcome = PromotionPolicy.GetPromotionOutcome(average, failed, 0); // absence not included here

                _students.Add(new GuardianStudentSummaryItem
                {
                    StudentId = student.StudentId,
                    FirstName = student.FirstName,
                    LastName = student.LastName,
                    RollNumber = student.RollNumber,
                    TotalScore = totalObtained,
                    AveragePercentage = average,
                    ResultText = outcome switch
                    {
                        PromotionOutcome.Promoted => "کامیاب",
                        PromotionOutcome.Conditional => "مشروط",
                        _ => "ناکام"
                    }
                });
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطا در بارگذاری شاگردان:\n{ex.Message}", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
