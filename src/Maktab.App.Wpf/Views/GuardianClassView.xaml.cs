using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Maktab.Application.Abstractions;
using Maktab.Domain.Entities;
using Maktab.Domain.Enums;

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
    private readonly IReportCardService _reportCardService;
    private readonly IAcademicYearService _academicYearService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IFinalizationService _finalizationService;
    private readonly IAuditService _auditService;
    private readonly IStudentAcademicEnrollmentRepository _enrollmentRepository;

    private readonly ObservableCollection<GuardianStudentSummaryItem> _students = [];
    private List<SchoolClass> _guardianClasses = [];

    public GuardianClassView(
        ITeacherAssignmentService assignmentService,
        IClassSubjectService classSubjectService,
        IStudentService studentService,
        IReportCardService reportCardService,
        IAcademicYearService academicYearService,
        ICurrentUserService currentUserService,
        IFinalizationService finalizationService,
        IAuditService auditService,
        IStudentAcademicEnrollmentRepository enrollmentRepository)
    {
        _assignmentService = assignmentService;
        _classSubjectService = classSubjectService;
        _studentService = studentService;
        _reportCardService = reportCardService;
        _academicYearService = academicYearService;
        _currentUserService = currentUserService;
        _finalizationService = finalizationService;
        _auditService = auditService;
        _enrollmentRepository = enrollmentRepository;

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
        await UpdateFinalizationStatusAsync();
    }

    private async void AcademicYearComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        await LoadStudentsAsync();
        await UpdateFinalizationStatusAsync();
    }

    private async Task LoadStudentsAsync()
    {
        if (ClassComboBox.SelectedValue is not int classId || classId <= 0) return;
        if (AcademicYearComboBox.SelectedValue is not int yearId || yearId <= 0) return;

        try
        {
            var enrollments = await _enrollmentRepository.GetByClassAndAcademicYearAsync(classId, yearId);

            _students.Clear();
            foreach (var enrollment in enrollments)
            {
                var student = await _studentService.GetStudentByIdAsync(enrollment.StudentId);
                if (student is null)
                    continue;

                var report = await _reportCardService.GetStudentReportCardDataAsync(
                    student.StudentId,
                    (await _academicYearService.GetAllAcademicYearsAsync())
                        .First(y => y.AcademicYearId == yearId).YearName);

                _students.Add(new GuardianStudentSummaryItem
                {
                    StudentId = report.StudentId,
                    FirstName = report.FirstName,
                    LastName = report.LastName,
                    RollNumber = report.RollNumber,
                    TotalScore = report.TotalObtainedScore,
                    AveragePercentage = report.AveragePercentage,
                    ResultText = report.PromotionOutcome switch
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

    private async Task UpdateFinalizationStatusAsync()
    {
        if (ClassComboBox.SelectedValue is not int classId || classId <= 0) return;
        if (AcademicYearComboBox.SelectedValue is not int yearId || yearId <= 0) return;

        try
        {
            bool isFinalized = await _finalizationService.IsClassFinalizedAsync(classId, yearId);
            FinalizationStatusTextBlock.Text = isFinalized ? "وضعیت: نهایی شده ✅" : "وضعیت: باز است 🔓";
            FinalizeButton.IsEnabled = !isFinalized;
            UnfinalizeButton.IsEnabled = isFinalized;
        }
        catch (Exception ex)
        {
            FinalizationStatusTextBlock.Text = "خطا در بررسی وضعیت";
            FinalizeButton.IsEnabled = false;
            UnfinalizeButton.IsEnabled = false;
        }
    }

    private async void FinalizeButton_Click(object sender, RoutedEventArgs e)
    {
        if (ClassComboBox.SelectedValue is not int classId || classId <= 0)
        {
            MessageBox.Show("لطفاً صنف را انتخاب کنید.", "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (AcademicYearComboBox.SelectedValue is not int yearId || yearId <= 0)
        {
            MessageBox.Show("لطفاً سال تعلیمی را انتخاب کنید.", "خطا", MessageBoxButton.OK, MessageBoxButton.OK == MessageBoxResult.Yes ? MessageBoxImage.Warning : MessageBoxImage.Warning);
            return;
        }

        var userId = _currentUserService.CurrentUser?.UserId;
        if (userId is null or <= 0)
            return;

        var confirm = MessageBox.Show(
            "آیا از نهایی کردن نتایج این صنف اطمینان دارید؟ پس از نهایی شدن، نمرات قابل ویرایش نخواهند بود.",
            "تأیید نهایی‌سازی",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirm != MessageBoxResult.Yes)
            return;

        try
        {
            await _finalizationService.FinalizeClassAsync(classId, yearId, userId.Value);
            await _auditService.LogAsync(_currentUserService.CurrentUser!.Username, $"نهایی‌سازی نتایج صنف {classId} سال {yearId}");
            await UpdateFinalizationStatusAsync();
            MessageBox.Show("نتایج با موفقیت نهایی شد.", "موفق", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void UnfinalizeButton_Click(object sender, RoutedEventArgs e)
    {
        if (ClassComboBox.SelectedValue is not int classId || classId <= 0) return;
        if (AcademicYearComboBox.SelectedValue is not int yearId || yearId <= 0) return;

        var userId = _currentUserService.CurrentUser?.UserId;
        if (userId is null or <= 0) return;

        var confirm = MessageBox.Show("آیا از بازگشایی نتایج این صنف اطمینان دارید؟", "تأیید بازگشایی", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (confirm != MessageBoxResult.Yes)
            return;

        try
        {
            await _finalizationService.UnfinalizeClassAsync(classId, yearId, userId.Value);
            await _auditService.LogAsync(_currentUserService.CurrentUser!.Username, $"بازگشایی نتایج صنف {classId} سال {yearId}");
            await UpdateFinalizationStatusAsync();
            MessageBox.Show("نتایج بازگشایی شد.", "موفق", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
