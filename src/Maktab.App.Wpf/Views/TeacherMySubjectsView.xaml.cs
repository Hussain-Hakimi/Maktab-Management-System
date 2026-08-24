using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Maktab.Application.Abstractions;

namespace Maktab.App.Wpf.Views;

public partial class TeacherMySubjectsView : UserControl
{
    private readonly ITeacherAssignmentService _assignmentService;
    private readonly ICurrentUserService _currentUserService;

    private readonly ObservableCollection<TeacherSubjectAssignmentDto> _subjects = [];
    private readonly ObservableCollection<ClassGuardianDto> _guardians = [];

    public TeacherMySubjectsView(
        ITeacherAssignmentService assignmentService,
        ICurrentUserService currentUserService)
    {
        _assignmentService = assignmentService;
        _currentUserService = currentUserService;

        InitializeComponent();
        SubjectsDataGrid.ItemsSource = _subjects;
        GuardianClassesDataGrid.ItemsSource = _guardians;
        Loaded += TeacherMySubjectsView_Loaded;
    }

    private async void TeacherMySubjectsView_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadAssignmentsAsync();
    }

    private async Task LoadAssignmentsAsync()
    {
        try
        {
            var userId = _currentUserService.CurrentUser?.UserId;
            if (userId is null or <= 0)
                return;

            var subjects = await _assignmentService.GetMyTeacherSubjectsAsync(userId.Value);
            _subjects.Clear();
            foreach (var item in subjects)
                _subjects.Add(item);

            var guardians = await _assignmentService.GetClassGuardiansAsync(userId.Value);
            _guardians.Clear();
            foreach (var item in guardians)
                _guardians.Add(item);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطا در بارگذاری تخصیص‌ها:\n{ex.Message}", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
