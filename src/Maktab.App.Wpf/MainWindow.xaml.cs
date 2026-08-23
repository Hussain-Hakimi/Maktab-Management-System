using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Maktab.App.Wpf.Services;
using Maktab.App.Wpf.Views;
using Maktab.Application.Abstractions;
using Maktab.Application.Services;
using Maktab.Domain.Enums;

namespace Maktab.App.Wpf;

public partial class MainWindow : Window
{
    private readonly NavigationService _navigationService;
    private readonly IUserService _userService;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUserService;

    private UserDto? _currentUser;

    // Existing view instances
    private readonly ClassSubjectView _classSubjectView;
    private readonly StudentManagementView _studentManagementView;
    private readonly MarksEntryView _marksEntryView;
    private readonly AttendanceView _attendanceView;
    private readonly LibraryView _libraryView;
    private readonly TextbookView _textbookView;
    private readonly FeesView _feesView;
    private readonly ReportCardsView _reportCardsView;
    private readonly BackupSettingsView _backupSettingsView;

    // New placeholder view instances
    private readonly DashboardView _dashboardView;
    private readonly UserManagementView _userManagementView;
    private readonly PromotionSettingsView _promotionSettingsView;
    private readonly BulkImportView _bulkImportView;
    private readonly AuditLogView _auditLogView;
    private readonly SchoolSettingsView _schoolSettingsView;
    private readonly AcademicYearView _academicYearView;

    // Allowed roles per sidebar item index (matching order in XAML)
    private static readonly UserRole[][] SidebarItemRoles =
    [
        [UserRole.Admin, UserRole.Teacher, UserRole.Librarian, UserRole.Accountant], // Dashboard
        [UserRole.Admin, UserRole.Teacher],                                           // Students
        [UserRole.Admin, UserRole.Teacher],                                           // Classes
        [UserRole.Admin, UserRole.Teacher],                                           // Marks
        [UserRole.Admin, UserRole.Teacher],                                           // Attendance
        [UserRole.Admin, UserRole.Librarian],                                         // Library
        [UserRole.Admin, UserRole.Librarian],                                         // Textbooks
        [UserRole.Admin, UserRole.Accountant],                                        // Fees
        [UserRole.Admin, UserRole.Teacher],                                           // Report Cards
        [UserRole.Admin],                                                             // Backup/Restore
        [UserRole.Admin],                                                             // User Management
        [UserRole.Admin],                                                             // Promotion Settings
        [UserRole.Admin],                                                             // Bulk Import
        [UserRole.Admin],                                                             // Audit Logs
        [UserRole.Admin],                                                             // School Settings
        [UserRole.Admin]                                                              // Academic Years
    ];

    public MainWindow(
        ClassSubjectView classSubjectView,
        StudentManagementView studentManagementView,
        MarksEntryView marksEntryView,
        AttendanceView attendanceView,
        LibraryView libraryView,
        TextbookView textbookView,
        FeesView feesView,
        ReportCardsView reportCardsView,
        BackupSettingsView backupSettingsView,
        DashboardView dashboardView,
        UserManagementView userManagementView,
        PromotionSettingsView promotionSettingsView,
        BulkImportView bulkImportView,
        AuditLogView auditLogView,
        SchoolSettingsView schoolSettingsView,
        AcademicYearView academicYearView,
        IUserService userService,
        IAuditService auditService,
        ICurrentUserService currentUserService)
    {
        InitializeComponent();

        _classSubjectView = classSubjectView;
        _studentManagementView = studentManagementView;
        _marksEntryView = marksEntryView;
        _attendanceView = attendanceView;
        _libraryView = libraryView;
        _textbookView = textbookView;
        _feesView = feesView;
        _reportCardsView = reportCardsView;
        _backupSettingsView = backupSettingsView;

        _dashboardView = dashboardView;
        _userManagementView = userManagementView;
        _promotionSettingsView = promotionSettingsView;
        _bulkImportView = bulkImportView;
        _auditLogView = auditLogView;
        _schoolSettingsView = schoolSettingsView;
        _academicYearView = academicYearView;

        _userService = userService;
        _auditService = auditService;
        _currentUserService = currentUserService;

        _navigationService = new NavigationService(MainContentArea);

        SidebarListBox.SelectedIndex = 0;
        Loaded += MainWindow_Loaded;
    }

    public void SetCurrentUser(UserDto user)
    {
        _currentUser = user;
        CurrentUserTextBlock.Text = $"👤 {user.FullName} ({user.Role})";
        try
        {
            ApplyRoleBasedSidebarVisibility();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error setting current user: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ApplyRoleBasedSidebarVisibility()
    {
        if (_currentUser is null) return;

        try
        {
            for (int i = 0; i < SidebarListBox.Items.Count; i++)
            {
                if (SidebarListBox.Items[i] is not ListBoxItem item)
                    continue;

                var allowedRoles = SidebarItemRoles[i];
                item.Visibility = allowedRoles.Contains(_currentUser.Role) ? Visibility.Visible : Visibility.Collapsed;
            }

            if (SidebarListBox.SelectedItem is ListBoxItem selectedItem && selectedItem.Visibility != Visibility.Visible)
            {
                SidebarListBox.SelectedIndex = 0;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error applying role-based sidebar: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            var persianCalendar = new PersianCalendar();
            var now = DateTime.Now;
            var shamsiYear = persianCalendar.GetYear(now);
            var shamsiMonth = persianCalendar.GetMonth(now);
            var shamsiDay = persianCalendar.GetDayOfMonth(now);

            CurrentDateTextBlock.Text = $"📅 {shamsiYear}/{shamsiMonth:D2}/{shamsiDay:D2} — {now:yyyy/MM/dd}";
            SchoolYearTextBlock.Text = $"سال تحصیلی: {AcademicYearProvider.GetCurrentAcademicYear(now)}";
        }
        catch
        {
            CurrentDateTextBlock.Text = $"📅 {DateTime.Now:yyyy/MM/dd}";
        }

        StatusBarText.Text = "✅ سیستم آماده استفاده است.";
    }

    private async void ChangePasswordButton_Click(object sender, RoutedEventArgs e)
    {
        if (_currentUser is null)
            return;

        var changePasswordWindow = new ChangePasswordWindow(_userService, _currentUser);
        var result = changePasswordWindow.ShowDialog();

        if (result == true)
        {
            await _auditService.LogAsync(_currentUser.Username, "تغییر رمز عبور");
            MessageBox.Show("رمز عبور با موفقیت تغییر کرد.", "موفق", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void SidebarListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SidebarListBox.SelectedIndex < 0) return;

        switch (SidebarListBox.SelectedIndex)
        {
            case 0: _navigationService.Navigate(_dashboardView); break;
            case 1: _navigationService.Navigate(_studentManagementView); break;
            case 2: _navigationService.Navigate(_classSubjectView); break;
            case 3: _navigationService.Navigate(_marksEntryView); break;
            case 4: _navigationService.Navigate(_attendanceView); break;
            case 5: _navigationService.Navigate(_libraryView); break;
            case 6: _navigationService.Navigate(_textbookView); break;
            case 7: _navigationService.Navigate(_feesView); break;
            case 8: _navigationService.Navigate(_reportCardsView); break;
            case 9: _navigationService.Navigate(_backupSettingsView); break;
            case 10: _navigationService.Navigate(_userManagementView); break;
            case 11: _navigationService.Navigate(_promotionSettingsView); break;
            case 12: _navigationService.Navigate(_bulkImportView); break;
            case 13: _navigationService.Navigate(_auditLogView); break;
            case 14: _navigationService.Navigate(_schoolSettingsView); break;
            case 15: _navigationService.Navigate(_academicYearView); break;
        }
    }
}
