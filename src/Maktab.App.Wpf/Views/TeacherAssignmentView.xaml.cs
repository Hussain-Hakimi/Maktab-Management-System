using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Maktab.Application.Abstractions;
using Maktab.Domain.Entities;

namespace Maktab.App.Wpf.Views;

public partial class TeacherAssignmentView : UserControl
{
    private readonly ITeacherAssignmentService _assignmentService;
    private readonly IUserService _userService;
    private readonly IClassSubjectService _classSubjectService;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAppLogger _logger;

    private readonly ObservableCollection<TeacherSubjectAssignmentDto> _teacherSubjects = [];
    private readonly ObservableCollection<ClassGuardianDto> _guardians = [];
    private List<UserDto> _teachers = [];

    public TeacherAssignmentView(
        ITeacherAssignmentService assignmentService,
        IUserService userService,
        IClassSubjectService classSubjectService,
        IAuditService auditService,
        ICurrentUserService currentUserService,
        IAppLogger logger)
    {
        _assignmentService = assignmentService;
        _userService = userService;
        _classSubjectService = classSubjectService;
        _auditService = auditService;
        _currentUserService = currentUserService;
        _logger = logger;

        InitializeComponent();
        TeacherSubjectsDataGrid.ItemsSource = _teacherSubjects;
        GuardiansDataGrid.ItemsSource = _guardians;
        Loaded += TeacherAssignmentView_Loaded;
    }

    private async void TeacherAssignmentView_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadTeachersAsync();
        await LoadClassesAsync();
        await LoadAssignmentsAsync();
    }

    private async Task LoadTeachersAsync()
    {
        try
        {
            var users = await _userService.GetAllUsersAsync();
            _teachers = users.Where(u => u.Role == Domain.Enums.UserRole.Teacher).ToList();

            TeacherComboBox.ItemsSource = _teachers;
            GuardianTeacherComboBox.ItemsSource = _teachers;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطا در بارگذاری استادان:\n{ex.Message}", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task LoadClassesAsync()
    {
        try
        {
            var classes = await _classSubjectService.GetClassesAsync();
            ClassComboBox.ItemsSource = classes;
            GuardianClassComboBox.ItemsSource = classes;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطا در بارگذاری صنف‌ها:\n{ex.Message}", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task LoadAssignmentsAsync()
    {
        try
        {
            var subjects = await _assignmentService.GetTeacherSubjectsAsync();
            _teacherSubjects.Clear();
            foreach (var item in subjects)
                _teacherSubjects.Add(item);

            var guardians = await _assignmentService.GetClassGuardiansAsync();
            _guardians.Clear();
            foreach (var item in guardians)
                _guardians.Add(item);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطا در بارگذاری تخصیص‌ها:\n{ex.Message}", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void ClassComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (TryGetSelectedId(ClassComboBox, out var classId))
        {
            try
            {
                var subjects = await _classSubjectService.GetSubjectsByClassAsync(classId);
                SubjectComboBox.ItemsSource = subjects;
                SubjectComboBox.SelectedIndex = subjects.Count > 0 ? 0 : -1;
                SubjectStatusTextBlock.Text = subjects.Count > 0
                    ? string.Empty
                    : "برای این صنف هنوز مضمونی ثبت نشده است.";
            }
            catch (Exception ex)
            {
                SubjectComboBox.ItemsSource = null;
                SubjectStatusTextBlock.Text = "بارگذاری مضامین انجام نشد.";
                MessageBox.Show($"خطا در بارگذاری مضامین:\n{ex.Message}", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        else
        {
            SubjectComboBox.ItemsSource = null;
            SubjectStatusTextBlock.Text = "ابتدا یک صنف را انتخاب کنید.";
        }
    }

    private async void AddTeacherSubjectButton_Click(object sender, RoutedEventArgs e)
    {
        if (!TryGetSelectedId(TeacherComboBox, out var teacherId))
        {
            MessageBox.Show("لطفاً استاد را انتخاب کنید.", "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (!TryGetSelectedId(ClassComboBox, out var classId))
        {
            MessageBox.Show("لطفاً صنف را انتخاب کنید.", "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (!TryGetSelectedId(SubjectComboBox, out var subjectId))
        {
            MessageBox.Show("لطفاً مضمون را انتخاب کنید.", "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            await _assignmentService.AssignTeacherToSubjectAsync(teacherId, classId, subjectId);
            await LogAuditAsync($"تخصیص مضمون به استاد");
            await LoadAssignmentsAsync();
            MessageBox.Show("تخصیص با موفقیت ثبت شد.", "موفق", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void RemoveTeacherSubjectButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is int teacherSubjectId)
        {
            try
            {
                await _assignmentService.RemoveTeacherSubjectAssignmentAsync(teacherSubjectId);
                await LogAuditAsync("حذف تخصیص مضمون");
                await LoadAssignmentsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }

    private async void AddGuardianButton_Click(object sender, RoutedEventArgs e)
    {
        if (!TryGetSelectedId(GuardianTeacherComboBox, out var teacherId))
        {
            MessageBox.Show("لطفاً استاد را انتخاب کنید.", "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (!TryGetSelectedId(GuardianClassComboBox, out var classId))
        {
            MessageBox.Show("لطفاً صنف را انتخاب کنید.", "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            await _assignmentService.AssignClassGuardianAsync(teacherId, classId);
            await LogAuditAsync("تخصیص نگران صنف");
            await LoadAssignmentsAsync();
            MessageBox.Show("نگران صنف با موفقیت ثبت شد.", "موفق", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void RemoveGuardianButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is int guardianId)
        {
            try
            {
                await _assignmentService.RemoveClassGuardianAsync(guardianId);
                await LogAuditAsync("حذف نگران صنف");
                await LoadAssignmentsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }

    private async Task LogAuditAsync(string action)
    {
        try
        {
            var userName = _currentUserService.CurrentUser?.Username ?? "Unknown";
            await _auditService.LogAsync(userName, action);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to write teacher-assignment audit entry for action '{action}'.", ex);
        }
    }

    private static bool TryGetSelectedId(ComboBox comboBox, out int id)
    {
        id = 0;
        if (comboBox.SelectedValue is null)
        {
            return false;
        }

        return int.TryParse(comboBox.SelectedValue.ToString(), out id) && id > 0;
    }
}
